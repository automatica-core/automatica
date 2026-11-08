using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Automatica.Core.Runtime.Core.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Core.Tests.Update
{
    /// <summary>
    /// Plugins are loaded into their own collectible load context, so a new version can be loaded without a restart
    /// </summary>
    public class PluginHotUpdateTests : IDisposable
    {
        private const string PluginName = "HotUpdateTestPlugin";
        private const string HelperName = "HotUpdateTestHelper";

        private readonly string _root = Path.Combine(Path.GetTempPath(), "automatica-hot-update-" + Guid.NewGuid().ToString("N"));

        private string CopyRoot => Path.Combine(_root, "copies");

        public PluginHotUpdateTests()
        {
            // the copies of reloaded plugins, not in the staging directory of the test run
            PluginLoadContexts.CopyRoot = CopyRoot;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, true);
            }
            catch
            {
                // still locked on windows if a context could not be collected
            }
        }

        [Fact]
        public void Plugin_IsLoadedIntoItsOwnCollectibleContext_AndSharesTheCoreTypes()
        {
            var folder = BuildPlugin("v1");
            // a copy of a core assembly in the plugin folder must not be used: the plugin has to see the core's types
            File.Copy(typeof(ServerInfo).Assembly.Location, Path.Combine(folder, Path.GetFileName(typeof(ServerInfo).Assembly.Location)));

            var result = Invoke(folder);

            Assert.Equal("v1", result.Version);
            Assert.Equal("helper v1", result.Helper);
            Assert.True(result.CoreTypeShared);
            Assert.True(result.Collectible);
            Assert.True(result.OwnContext);
            Assert.True(result.HelperInOwnContext);

            Assert.True(PluginLoadContext.IsHostAssembly(typeof(ServerInfo).Assembly.GetName()));
            Assert.False(PluginLoadContext.IsHostAssembly(new AssemblyName(PluginName)));

            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public void Plugin_CanBeUnloaded_AndANewVersionLoadedFromTheSameFolder()
        {
            var folder = BuildPlugin("v1");
            Assert.Equal("v1", Invoke(folder).Version);

            var unloaded = PluginLoadContexts.Unload(folder);
            Assert.False(PluginLoadContexts.Contains(folder));
            Assert.True(PluginUpdater.WaitForUnload(unloaded, 20), "the old plugin version could not be unloaded");

            // the files are free again (windows locks them while they are loaded)
            BuildPlugin("v2");

            var result = Invoke(folder);
            Assert.Equal("v2", result.Version);
            Assert.Equal("helper v2", result.Helper);

            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public void Reload_IsLoadedFromACopy_OfTheWholePluginDirectory()
        {
            var folder = BuildPlugin("v1");
            Directory.CreateDirectory(Path.Combine(folder, "runtimes", "linux-x64", "native"));
            File.WriteAllText(Path.Combine(folder, "runtimes", "linux-x64", "native", "lib.so"), "native");

            var first = PluginLoadContexts.GetOrCreate(folder);
            Assert.Equal(first.Folder, first.LoadFolder);
            // copied whether or not the old context is collected
            PluginLoadContexts.Unload(folder);

            var second = PluginLoadContexts.GetOrCreate(folder);
            Assert.Equal(PluginLoadContexts.Normalize(folder), second.Folder);
            Assert.NotEqual(second.Folder, second.LoadFolder);
            Assert.StartsWith(Path.GetFullPath(CopyRoot), Path.GetFullPath(second.LoadFolder));
            Assert.True(File.Exists(Path.Combine(second.LoadFolder, PluginName + ".dll")));
            Assert.True(File.Exists(Path.Combine(second.LoadFolder, "runtimes", "linux-x64", "native", "lib.so")));
            Assert.Equal(Path.Combine(second.LoadFolder, PluginName + ".dll"), second.ToLoadPath(Path.Combine(folder, PluginName + ".dll")));
            // a file outside the plugin directory is not mapped
            var outside = Path.Combine(_root, "other.dll");
            Assert.Equal(outside, second.ToLoadPath(outside));

            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public void Reload_WhileTheOldVersionIsStillReferenced_LoadsTheNewVersion()
        {
            var folder = BuildPlugin("v1");
            Assert.Equal("v1", Invoke(folder).Version);
            Assert.True(PluginUpdater.WaitForUnload(PluginLoadContexts.Unload(folder), 20));

            BuildPlugin("v2");
            var (version, keep) = LoadAndKeep(folder);
            Assert.Equal("v2", version);

            // the old version can not be unloaded, e.g. a static event still holds it (seen on the NAS for Time, Operations and TextToSpeech)
            var unloaded = PluginLoadContexts.Unload(folder);
            Assert.False(PluginUpdater.WaitForUnload(unloaded, 3));

            // the reloaded version was loaded from a copy: the installer can replace the plugin directory
            BuildPlugin("v3");
            var result = Invoke(folder);
            Assert.Equal("v3", result.Version);
            Assert.Equal("helper v3", result.Helper);

            GC.KeepAlive(keep);
            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public void Reload_AfterTheFilesWereReplaced_WhileTheFirstVersionIsStillReferenced_LoadsTheNewVersion()
        {
            if (OperatingSystem.IsWindows())
            {
                // windows locks the files of the first (in place) load, the update waits for the unload there
                return;
            }

            var folder = BuildPlugin("v1");
            var (version, keep) = LoadAndKeep(folder);
            Assert.Equal("v1", version);
            PluginLoadContexts.Unload(folder);

            // like the installer: delete the directory and copy the new files
            Directory.Delete(folder, true);
            BuildPlugin("v2");

            // without the copy the runtime returns the cached v1 image for the same path
            Assert.Equal("v2", Invoke(folder).Version);

            GC.KeepAlive(keep);
            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public void CopiesOfEarlierProcesses_AreDeleted_OwnAndLaterOnesAreKept()
        {
            var root = CreateFolder("copies-cleanup");
            foreach (var name in new[] { "100-1", "200-2", "300-3", "foreign" })
            {
                Directory.CreateDirectory(Path.Combine(root, name, "Time-1"));
            }

            PluginLoadContexts.DeleteCopiesOfEarlierProcesses(root, "200-2");

            Assert.Equal(new[] { "200-2", "300-3", "foreign" }, Directory.GetDirectories(root).Select(Path.GetFileName).OrderBy(a => a).ToArray());
        }

        // keeps an instance of the plugin, so its context can not be collected
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string Version, object Instance) LoadAndKeep(string folder)
        {
            var context = PluginLoadContexts.GetOrCreate(folder);
            var assembly = context.LoadPluginAssembly(Path.Combine(folder, PluginName + ".dll"));
            var type = assembly.GetType("HotUpdate.Instance")!;
            var instance = Activator.CreateInstance(type)!;
            return ((string)type.GetMethod("Version")!.Invoke(instance, null)!, instance);
        }

        [Fact]
        public void TwoVersions_CanBeLoadedSideBySide()
        {
            var v1 = BuildPlugin("v1", "a");
            var v2 = BuildPlugin("v2", "b");

            Assert.Equal("v1", Invoke(v1).Version);
            Assert.Equal("v2", Invoke(v2).Version);

            PluginLoadContexts.Unload(v1);
            PluginLoadContexts.Unload(v2);
        }

        [Fact]
        public async Task Update_NotStarted_RequiresRestart_AndKeepsThePackage()
        {
            var target = new FakeTarget { IsStarted = false };
            var package = CreatePackage();

            var summary = await new PluginUpdater(target, NullLogger.Instance).UpdateAll(new[] { new PluginUpdate(CreatePlugin(), package) }, _ => CreateFolder("x"));

            Assert.True(summary.RestartRequired);
            Assert.True(File.Exists(package));
            Assert.Empty(target.Calls);
        }

        [Fact]
        public async Task Update_UntrustedPackage_IsNotInstalled_AndNoRestart()
        {
            var target = new FakeTarget { VerifyFails = true };
            var package = CreatePackage();
            var folder = CreateFolder("loaded");
            PluginLoadContexts.GetOrCreate(folder);

            var summary = await new PluginUpdater(target, NullLogger.Instance).UpdateAll(new[] { new PluginUpdate(CreatePlugin(), package) }, _ => folder);

            Assert.False(summary.RestartRequired);
            Assert.Equal(package, Assert.Single(summary.Failed).PackageFile);
            Assert.False(File.Exists(package));
            Assert.Equal(new[] { "verify" }, target.Calls);
            Assert.True(PluginLoadContexts.Contains(folder));
            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public async Task Update_PluginNotLoaded_IsInstalledAndLoaded_WithoutStoppingAnything()
        {
            var target = new FakeTarget();

            var result = await new PluginUpdater(target, NullLogger.Instance).Update(CreatePlugin(), CreatePackage(), CreateFolder("new"));

            Assert.Equal(PluginUpdateResult.Updated, result);
            Assert.Equal(new[] { "verify", "install", "load" }, target.Calls);
        }

        [Fact]
        public async Task Update_PluginInUse_StopsUnloadsInstallsLoadsAndStartsItsInstances()
        {
            var target = new FakeTarget();
            var folder = CreateFolder("loaded");
            var context = PluginLoadContexts.GetOrCreate(folder);
            var package = CreatePackage();

            var summary = await new PluginUpdater(target, NullLogger.Instance).UpdateAll(new[] { new PluginUpdate(CreatePlugin(), package) }, _ => folder);

            Assert.False(summary.RestartRequired);
            Assert.Empty(summary.Failed);
            Assert.False(File.Exists(package));
            Assert.Equal(new[] { "verify", "stop", "install", "load", "start" }, target.Calls);
            Assert.Same(context, target.StoppedContext);
            Assert.Same(target.Instances, target.StartedInstances);
            // the old context is unloaded, the load of the new version creates a new one
            Assert.False(PluginLoadContexts.Contains(folder));
        }

        [Fact]
        public async Task Update_InstallFails_RestartsTheOldVersion_WithoutRestart()
        {
            var target = new FakeTarget { InstallResult = false };
            var folder = CreateFolder("loaded");
            PluginLoadContexts.GetOrCreate(folder);

            var result = await new PluginUpdater(target, NullLogger.Instance).Update(CreatePlugin(), CreatePackage(), folder);

            Assert.Equal(PluginUpdateResult.Failed, result);
            Assert.Equal(new[] { "verify", "stop", "install", "load", "start" }, target.Calls);
        }

        [Fact]
        public async Task Update_InstancesCanNotBeStarted_RequiresRestart_AndKeepsThePackage()
        {
            var target = new FakeTarget { StartResult = false };
            var folder = CreateFolder("loaded");
            PluginLoadContexts.GetOrCreate(folder);
            var package = CreatePackage();

            var summary = await new PluginUpdater(target, NullLogger.Instance).UpdateAll(new[] { new PluginUpdate(CreatePlugin(), package) }, _ => folder);

            Assert.True(summary.RestartRequired);
            Assert.True(File.Exists(package));
        }

        [Fact]
        public async Task Update_PluginWithNativeLibraries_RequiresRestart_WithoutStoppingAnything()
        {
            var target = new FakeTarget();
            var folder = CreateFolder("native");
            PluginLoadContexts.GetOrCreate(folder).NativeLibrariesLoaded = true;

            var result = await new PluginUpdater(target, NullLogger.Instance).Update(CreatePlugin(), CreatePackage(), folder);

            Assert.Equal(PluginUpdateResult.RestartRequired, result);
            Assert.Equal(new[] { "verify" }, target.Calls);
            Assert.True(PluginLoadContexts.Contains(folder));
            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public async Task Update_TargetThrows_RequiresRestart()
        {
            var target = new FakeTarget { StopThrows = true };
            var folder = CreateFolder("loaded");
            PluginLoadContexts.GetOrCreate(folder);

            var summary = await new PluginUpdater(target, NullLogger.Instance).UpdateAll(new[] { new PluginUpdate(CreatePlugin(), CreatePackage()) }, _ => folder);

            Assert.True(summary.RestartRequired);
            PluginLoadContexts.Unload(folder);
        }

        [Fact]
        public void Library_ThePluginDoesNotShip_IsLoadedFromAnotherPluginFolder_AsItsOwnCopy()
        {
            // like shelly using PureWebSockets that only the loxone folder has
            var plugin = BuildPlugin("v1", "Drivers/shelly");
            var other = CreateFolder("Drivers/loxone");
            File.Move(Path.Combine(plugin, HelperName + ".dll"), Path.Combine(other, HelperName + ".dll"));

            var (helper, helperFolder, ownContext, weak) = InvokeHelper(plugin, _root);

            Assert.Equal("helper v1", helper);
            Assert.Equal(Path.GetFullPath(other), Path.GetFullPath(helperFolder));
            Assert.True(ownContext);
            Assert.True(PluginUpdater.WaitForUnload(weak, 20));
        }

        [Fact]
        public void Library_ThePluginDoesNotShip_IsNotFound_WithoutSharedRoot_OrInALowerVersion()
        {
            var plugin = BuildPlugin("v1", "Drivers/shelly");
            var other = CreateFolder("Drivers/loxone");
            File.Move(Path.Combine(plugin, HelperName + ".dll"), Path.Combine(other, HelperName + ".dll"));

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeHelper(plugin, null));
            Assert.IsType<FileNotFoundException>(ex.InnerException);

            var context = new PluginLoadContext(plugin, _root);
            Assert.Equal(Path.GetFullPath(Path.Combine(other, HelperName + ".dll")), Path.GetFullPath(context.FindInOtherPlugins(new AssemblyName(HelperName))!));
            Assert.Null(context.FindInOtherPlugins(new AssemblyName(HelperName) { Version = new Version(99, 0) }));
            context.Unload();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string Helper, string HelperFolder, bool OwnContext, WeakReference Context) InvokeHelper(string folder, string? sharedRoot)
        {
            var context = new PluginLoadContext(folder, sharedRoot);
            var weak = new WeakReference(context);
            try
            {
                var assembly = context.LoadPluginAssembly(Path.Combine(folder, PluginName + ".dll"));
                var helper = (string)assembly.GetType("HotUpdate.Plugin")!.GetMethod("Helper")!.Invoke(null, null)!;
                var helperAssembly = context.Assemblies.Single(a => a.GetName().Name == HelperName);
                return (helper, Path.GetDirectoryName(helperAssembly.Location)!, AssemblyLoadContext.GetLoadContext(helperAssembly) == context, weak);
            }
            finally
            {
                context.Unload();
            }
        }

        private record InvokeResult(string Version, string Helper, bool CoreTypeShared, bool Collectible, bool OwnContext, bool HelperInOwnContext);

        // no reference to the plugin's types may stay on the test method's stack, otherwise it can not be unloaded
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvokeResult Invoke(string folder)
        {
            var context = PluginLoadContexts.GetOrCreate(folder);
            var assembly = context.LoadPluginAssembly(Path.Combine(folder, PluginName + ".dll"));
            var type = assembly.GetType("HotUpdate.Plugin")!;

            var version = (string)type.GetMethod("Version")!.Invoke(null, null)!;
            var helper = (string)type.GetMethod("Helper")!.Invoke(null, null)!;
            var coreType = (Type)type.GetMethod("CoreType")!.Invoke(null, null)!;
            var helperAssembly = context.Assemblies.Single(a => a.GetName().Name == HelperName);

            return new InvokeResult(version, helper, coreType == typeof(ServerInfo), context.IsCollectible,
                AssemblyLoadContext.GetLoadContext(assembly) == context, AssemblyLoadContext.GetLoadContext(helperAssembly) == context);
        }

        private string BuildPlugin(string version, string folderName = "plugin")
        {
            var folder = Path.Combine(_root, folderName);
            Directory.CreateDirectory(folder);

            var helperPath = Path.Combine(folder, HelperName + ".dll");
            Compile(HelperName, $"namespace HotUpdate {{ public static class Helper {{ public static string Text() => \"helper {version}\"; }} }}", helperPath);

            Compile(PluginName, $@"
namespace HotUpdate
{{
    public static class Plugin
    {{
        public static string Version() => ""{version}"";
        public static string Helper() => HotUpdate.Helper.Text();
        public static System.Type CoreType() => typeof(Automatica.Core.Base.Common.ServerInfo);
    }}

    public class Instance
    {{
        public string Version() => ""{version}"";
    }}
}}", Path.Combine(folder, PluginName + ".dll"), helperPath);

            return folder;
        }

        private static void Compile(string name, string source, string output, params string[] references)
        {
            var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(a => Path.GetFileName(a).StartsWith("System.") || Path.GetFileName(a) is "mscorlib.dll" or "netstandard.dll")
                .Select(a => MetadataReference.CreateFromFile(a));

            var compilation = CSharpCompilation.Create(name,
                new[] { CSharpSyntaxTree.ParseText(source) },
                tpa.Concat(references.Select(a => MetadataReference.CreateFromFile(a)))
                    .Append(MetadataReference.CreateFromFile(typeof(ServerInfo).Assembly.Location)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var emit = compilation.Emit(output);
            Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        }

        private string CreateFolder(string name)
        {
            var folder = Path.Combine(_root, name);
            Directory.CreateDirectory(folder);
            return folder;
        }

        private string CreatePackage()
        {
            Directory.CreateDirectory(_root);
            var file = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".acpkg");
            File.WriteAllText(file, "package");
            return file;
        }

        private static Plugin CreatePlugin()
        {
            return new Plugin { ComponentName = "TestPlugin", PluginGuid = Guid.NewGuid(), PluginType = PluginType.Driver };
        }

        private class FakeTarget : IPluginUpdateTarget
        {
            public bool IsStarted { get; set; } = true;
            public bool VerifyFails { get; set; }
            public bool StopThrows { get; set; }
            public bool InstallResult { get; set; } = true;
            public bool StartResult { get; set; } = true;

            public List<string> Calls { get; } = new();
            public PluginLoadContext? StoppedContext { get; private set; }
            public PluginInstances Instances { get; } = new(new[] { Guid.NewGuid() }, new[] { Guid.NewGuid() });
            public PluginInstances? StartedInstances { get; private set; }

            public void VerifyPackage(Plugin plugin, string packageFile)
            {
                Calls.Add("verify");
                if (VerifyFails)
                {
                    throw new InvalidOperationException("not signed");
                }
            }

            public Task<PluginInstances> StopAndRelease(PluginLoadContext context, Plugin plugin)
            {
                Calls.Add("stop");
                if (StopThrows)
                {
                    throw new InvalidOperationException("stop failed");
                }
                StoppedContext = context;
                return Task.FromResult(Instances);
            }

            public Task<bool> Install(Plugin plugin, string packageFile)
            {
                Calls.Add("install");
                return Task.FromResult(InstallResult);
            }

            public Task Load(Plugin plugin)
            {
                Calls.Add("load");
                return Task.CompletedTask;
            }

            public Task<bool> Start(PluginInstances instances)
            {
                Calls.Add("start");
                StartedInstances = instances;
                return Task.FromResult(StartResult);
            }
        }
    }
}
