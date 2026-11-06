using System;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Common.Update;
using Automatica.Core.Common.Update.Signing;
using Newtonsoft.Json;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin = Automatica.Core.EF.Models.Plugin;

namespace Automatica.Core.Runtime.Core.Plugins
{
    internal class PluginInstaller : IPluginInstaller
    {
        private readonly ILogger _logger;

        public PluginInstaller(IConfiguration config, ILogger<PluginInstaller> logger)
        {
            _logger = logger;
        }
        public Task<bool> InstallPlugin(Plugin plugin, string acPkgFilePath)
        {
            return InstallPlugin(plugin.PluginType, plugin.ComponentName, acPkgFilePath);
        }

        public Task<bool> InstallPlugin(PluginManifest plugin, string acPkgFilePath)
        {
            PluginType pluginType;

            switch (plugin.Automatica.Type)
            {
                case "driver":
                    pluginType = PluginType.Driver;
                    break;
                case "rule":
                    pluginType = PluginType.Logic;
                    break;
                default:
                    throw new NotImplementedException($"PluginType {plugin.Automatica.Type} is not supported");
            }

            return InstallPlugin(pluginType, plugin.Automatica.ComponentName, acPkgFilePath);
        }

        private Task<bool> InstallPlugin(PluginType pluginType, string componentName, string acPkgFilePath)
        {

            _logger.LogDebug($"Start install plugin {componentName} from file {acPkgFilePath}");

            var pluginRoot = Path.GetFullPath(Path.Combine(ServerInfo.PluginDirectory,
                pluginType == PluginType.Driver
                    ? ServerInfo.DriversDirectory
                    : ServerInfo.LogicsDirectory));

            // the component name becomes a directory that is deleted recursively: it must be a plain name below the plugin directory
            if (!IsValidComponentName(componentName) ||
                !Path.GetFullPath(Path.Combine(pluginRoot, componentName)).StartsWith(pluginRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                _logger.LogError($"Invalid plugin component name {componentName}, plugin is not installed");
                return Task.FromResult(false);
            }

            var tmpDirectory = ServerInfo.CreatePrivateTempDirectory();

            try
            {
                var installDir = Path.Combine(pluginRoot, componentName);

                // verify (signature, anti-rollback) and extract before the installed version is removed
                _logger.LogDebug($"Extract plugin to {tmpDirectory}");
                var signature = Common.Update.Plugin.InstallPlugin(_logger, acPkgFilePath, tmpDirectory, componentName);
                PackageVerifier.EnsureNoDowngrade(signature, GetInstalledSignedVersion(installDir), componentName, _logger);

                var signatureFile = Path.Combine(tmpDirectory, PackageSignatureFormat.SignatureFileName);
                if (signature != null)
                {
                    // the extracted files must be the signed ones
                    var extracted = PackageVerifier.VerifyDirectory(Path.Combine(tmpDirectory, componentName), PackageSignatureFormat.TypePlugin,
                        componentName, componentName + "/", false, true, signatureFile);
                    PackageVerifier.EnsureTrusted(extracted, componentName, _logger);
                }

                if (Directory.Exists(installDir))
                {
                    Directory.Delete(installDir, true);
                }

                if (!Directory.Exists(installDir))
                {
                    _logger.LogDebug($"Create target directory {installDir}");
                    Directory.CreateDirectory(installDir);
                }

                foreach (var file in Directory.GetFiles(Path.Combine(tmpDirectory, componentName)))
                {
                    var fileInfo = new FileInfo(file);

                    var destination = Path.Combine(installDir, fileInfo.Name);

                    _logger.LogDebug($"Copy file from {file} to {destination}");
                    File.Copy(file, destination, true);
                }

                // kept with the plugin: checked when the plugin is loaded and for the anti-rollback check of the next install
                if (signature != null)
                {
                    File.Copy(signatureFile, Path.Combine(installDir, PackageSignatureFormat.SignatureFileName), true);
                }
                _logger.LogDebug($"Done install plugin...");
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not install plugin...");
            }
            finally
            {
                Directory.Delete(tmpDirectory, true);
            }

            return Task.FromResult(false);
        }

        /// <returns>the signed version of the installed plugin, null if it is not installed or has no signature</returns>
        private string GetInstalledSignedVersion(string installDir)
        {
            var file = Path.Combine(installDir, PackageSignatureFormat.SignatureFileName);
            if (!File.Exists(file))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<PackageSignature>(File.ReadAllText(file))?.Version;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Could not read {file}");
                return null;
            }
        }

        internal static bool IsValidComponentName(string componentName)
        {
            return !string.IsNullOrWhiteSpace(componentName) &&
                   componentName != "." && componentName != ".." &&
                   Path.GetFileName(componentName) == componentName &&
                   componentName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   componentName.IndexOfAny(new[] { '/', '\\', ':' }) < 0;
        }
    }
}
