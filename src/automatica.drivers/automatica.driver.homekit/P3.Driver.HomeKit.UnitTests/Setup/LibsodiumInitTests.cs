using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Setup
{
    /// <summary>
    /// <see cref="HomeKitServer.Init"/> runs in its own load context like a plugin: in the test host NSec already uses the
    /// libsodium of its runtimes folder, binding the remaining NSec imports of that instance to a second copy crashes the process.
    /// </summary>
    public class LibsodiumInitTests
    {
        private static string AssemblyDirectory => Path.GetDirectoryName(typeof(HomeKitServer).Assembly.Location)!;

        private sealed class PluginContext : AssemblyLoadContext
        {
            private readonly string _directory;

            public PluginContext(string directory) : base("homekit-libsodium-test", true)
            {
                _directory = directory;
            }

            public List<string> UnmanagedRequests { get; } = new();

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                // the plugin and its NSec, everything else (logging abstractions, core) is shared with the host
                if (assemblyName.Name != null && (assemblyName.Name.StartsWith("P3.Driver.HomeKit") || assemblyName.Name.StartsWith("NSec")))
                {
                    return LoadFromAssemblyPath(Path.Combine(_directory, assemblyName.Name + ".dll"));
                }
                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                // the plugin loader resolves native libraries from the deps.json, record that it is not asked for libsodium
                UnmanagedRequests.Add(unmanagedDllName);
                return IntPtr.Zero;
            }
        }

        private sealed class InitResult
        {
            public string? LoadedPath;
            public int Major;
            public byte[]? RandomBytes;
            public PluginContext? Context;
            public Assembly? NSec;
        }

        private static InitResult InitInPluginContext()
        {
            var context = new PluginContext(AssemblyDirectory);
            var homeKit = context.LoadFromAssemblyPath(Path.Combine(AssemblyDirectory, "P3.Driver.HomeKit.dll"));
            Assert.NotSame(typeof(HomeKitServer).Assembly, homeKit);

            var server = homeKit.GetType(typeof(HomeKitServer).FullName!)!;
            server.GetMethod(nameof(HomeKitServer.Init))!.Invoke(null, new object[] { NullLogger.Instance });
            server.GetMethod(nameof(HomeKitServer.Init))!.Invoke(null, new object[] { NullLogger.Instance }); // drivers call it once per start

            var nsec = context.LoadFromAssemblyName(new AssemblyName("NSec.Cryptography"));
            var rngType = nsec.GetType("NSec.Cryptography.RandomGenerator")!;
            var rng = rngType.GetProperty("Default")!.GetValue(null);

            return new InitResult
            {
                LoadedPath = (string?)server.GetProperty(nameof(HomeKitServer.LoadedLibsodiumPath), BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null),
                Major = (int)server.GetMethod(nameof(HomeKitServer.sodium_library_version_major), BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!,
                RandomBytes = (byte[]?)rngType.GetMethod("GenerateBytes", new[] { typeof(int) })!.Invoke(rng, new object[] { 32 }),
                Context = context,
                NSec = nsec
            };
        }

        [Fact]
        public void Init_DoesNotWriteIntoThePluginDirectory()
        {
            // the plugin directory is signed: any extra file (libsodium.so) makes the core refuse the plugin on the next start
            var before = DateTime.UtcNow.AddSeconds(-1);

            var result = InitInPluginContext();

            var written = Directory.EnumerateFiles(AssemblyDirectory, "libsodium*", SearchOption.TopDirectoryOnly)
                .Where(a => File.GetLastWriteTimeUtc(a) >= before)
                .Select(Path.GetFileName)
                .ToList();
            Assert.Empty(written);

            Assert.NotNull(result.LoadedPath);
            Assert.False(Path.GetFullPath(result.LoadedPath!).StartsWith(Path.GetFullPath(AssemblyDirectory), StringComparison.OrdinalIgnoreCase));
            Assert.StartsWith(HomeKitServer.TempDirectoryPrefix, Path.GetFileName(Path.GetDirectoryName(result.LoadedPath)));

            if (!OperatingSystem.IsWindows())
            {
                Assert.False(File.Exists(result.LoadedPath), "the extracted copy is removed once it is loaded");
            }
        }

        [Fact]
        public void Init_LibsodiumIsUsableByTheDriverAndNSec()
        {
            var result = InitInPluginContext();

            Assert.True(result.Major > 0);
            Assert.Equal(32, result.RandomBytes!.Length);

            // NSec of the plugin context, bound by the resolver of Init and not by the load context
            Assert.Same(result.Context, AssemblyLoadContext.GetLoadContext(result.NSec!));
            Assert.DoesNotContain(result.Context!.UnmanagedRequests, a => a.Contains("sodium", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("libsodium", true)]
        [InlineData("libsodium.so", true)]
        [InlineData("libsodium.dll", true)]
        [InlineData("libsodium.so.23", true)]
        [InlineData("libssl", false)]
        [InlineData("libsodiumx", false)]
        public void IsLibsodium(string name, bool expected)
        {
            Assert.Equal(expected, HomeKitServer.IsLibsodium(name));
        }
    }
}
