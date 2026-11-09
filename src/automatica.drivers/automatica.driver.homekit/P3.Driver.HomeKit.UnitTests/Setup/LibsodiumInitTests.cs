using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NSec.Cryptography;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Setup
{
    public class LibsodiumInitTests
    {
        private static string AssemblyDirectory => Path.GetDirectoryName(typeof(HomeKitServer).Assembly.Location)!;

        [Fact]
        public void Init_DoesNotWriteIntoThePluginDirectory()
        {
            // the plugin directory is signed: any extra file (libsodium.so) makes the core refuse the plugin on the next start
            var before = DateTime.UtcNow.AddSeconds(-1);

            HomeKitServer.Init(NullLogger.Instance);

            var written = Directory.EnumerateFiles(AssemblyDirectory, "libsodium*", SearchOption.TopDirectoryOnly)
                .Where(a => File.GetLastWriteTimeUtc(a) >= before)
                .Select(Path.GetFileName)
                .ToList();
            Assert.Empty(written);

            Assert.NotNull(HomeKitServer.LoadedLibsodiumPath);
            Assert.False(Path.GetFullPath(HomeKitServer.LoadedLibsodiumPath).StartsWith(Path.GetFullPath(AssemblyDirectory), StringComparison.OrdinalIgnoreCase));
            Assert.StartsWith(HomeKitServer.TempDirectoryPrefix, Path.GetFileName(Path.GetDirectoryName(HomeKitServer.LoadedLibsodiumPath)));

            if (!OperatingSystem.IsWindows())
            {
                Assert.False(File.Exists(HomeKitServer.LoadedLibsodiumPath), "the extracted copy is removed once it is loaded");
            }
        }

        [Fact]
        public void Init_LibsodiumIsUsable()
        {
            HomeKitServer.Init(NullLogger.Instance);
            HomeKitServer.Init(NullLogger.Instance); // drivers call it once per start

            Assert.True(HomeKitServer.sodium_library_version_major() > 0);

            using (var key = Key.Create(SignatureAlgorithm.Ed25519))
            {
                var data = new byte[] { 1, 2, 3 };
                var signature = SignatureAlgorithm.Ed25519.Sign(key, data);
                Assert.True(SignatureAlgorithm.Ed25519.Verify(key.PublicKey, data, signature));
            }
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
