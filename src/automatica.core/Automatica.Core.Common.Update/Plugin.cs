using Automatica.Core.Base.Common;
using Automatica.Core.Common.Update.Signing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Automatica.Core.Common.Update
{
    public static class Plugin
    {
        public const string ManifestFileName = "automatica-manifest.json";

        /// <summary>
        /// Verifies the plugin package signature and extracts it to <paramref name="installDirectory"/>
        /// </summary>
        /// <param name="componentName">the expected component name, null to accept any signed name</param>
        /// <returns>the verified signature, null if an unsigned package was accepted by the policy</returns>
        /// <exception cref="PackageVerificationException">the package must not be installed</exception>
        public static PackageSignature InstallPlugin(ILogger logger, string pluginFile, string installDirectory, string componentName)
        {
            var signature = VerifyPluginPackage(logger, pluginFile, componentName);

            ZipFile.ExtractToDirectory(pluginFile, installDirectory, true);
            File.Delete(Path.Combine(installDirectory, ManifestFileName));
            return signature;
        }

        /// <summary>
        /// Verifies the signature of a plugin package before it is extracted
        /// </summary>
        /// <returns>the signature, null if an unsigned package was accepted by the policy</returns>
        /// <exception cref="PackageVerificationException">the package must not be installed</exception>
        public static PackageSignature VerifyPluginPackage(ILogger logger, string pluginFile, string componentName)
        {
            var result = PackageVerifier.VerifyZip(pluginFile, PackageSignatureFormat.TypePlugin, componentName);
            return PackageVerifier.EnsureTrusted(result, Path.GetFileName(pluginFile), logger);
        }

        public static PluginManifest GetEmbeddedPluginManifest(ILogger logger, Assembly assembly)
        {
            var resources = assembly.GetManifestResourceNames().SingleOrDefault(a => a.EndsWith(ManifestFileName));
            if(resources == null)
            {
                logger.LogWarning($"{assembly} does not contain a manifest files");
                return null;
            }
            return LoadManifest(logger, assembly.GetManifestResourceStream(resources));
        }
        public static bool CheckPluginFile(ILogger logger, string fileName, bool checkVersion)
        {
            var tempPath = ServerInfo.CreatePrivateTempDirectory();
            var check = CheckPluginFileInternal(logger, tempPath, fileName, checkVersion);

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            return check;
        }

        /// <summary>
        /// Verifies the signature of the plugin package and extracts it to <paramref name="tempPath"/>
        /// </summary>
        /// <returns>the manifest, null if the package is invalid or its signature is not trusted</returns>
        public static PluginManifest GetPluginManifest(ILogger logger, string fileName, string tempPath)
        {
            var manifest = GetPluginManifestInternal(logger, tempPath, fileName);
            return manifest;
        }

        private static PluginManifest GetPluginManifestInternal(ILogger logger, string tempPath, string fileName)
        {
            PackageSignature signature;
            try
            {
                signature = VerifyPluginPackage(logger, fileName, null);
            }
            catch (PackageVerificationException e)
            {
                logger.LogError(e.Message);
                return null;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not verify the plugin file");
                return null;
            }

            try
            {
                ZipFile.ExtractToDirectory(fileName, tempPath, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not unzip update file");
                return null;
            }

            var manifest = Path.Combine(tempPath, ManifestFileName);

            if (!File.Exists(manifest))
            {
                logger.LogError("Could not find automatica-manifest.json file");
                return null;
            }

            PluginManifest pluginManifest;
            using (var reader = new StreamReader(manifest))
            {
                pluginManifest = LoadManifest(logger, reader.BaseStream);
            }

            if (signature != null && pluginManifest?.Automatica != null)
            {
                // old manifests have no plugin version, the signed version is the one that counts
                if (signature.Name != pluginManifest.Automatica.ComponentName || !Version.TryParse(signature.Version, out var signedVersion) ||
                    (pluginManifest.Automatica.PluginVersion != null && signedVersion != pluginManifest.Automatica.PluginVersion))
                {
                    logger.LogError($"Plugin manifest {pluginManifest.Automatica.ComponentName} {pluginManifest.Automatica.PluginVersion} does not match the signed {signature.Name} {signature.Version}");
                    return null;
                }
            }

            return pluginManifest;
        }

        private static PluginManifest LoadManifest(ILogger logger, Stream stream)
        {
            var manifestStr = "";
            using (var reader = new StreamReader(stream))
            {
                manifestStr = reader.ReadToEnd();
            }
            try
            {
                var pluginManifest = JsonConvert.DeserializeObject<PluginManifest>(manifestStr);

                return pluginManifest;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception occured while checking the update package");
                return null;
            }
        }

        private static bool CheckPluginFileInternal(ILogger logger, string tempPath, string fileName, bool checkVersion)
        {
            try
            {
                var pluginManifest = GetPluginManifestInternal(logger, tempPath, fileName);

                if(pluginManifest == null)
                {
                    return false;
                }

                // a server with exactly the minimum version is fine
                if (checkVersion && new Version(ServerInfo.GetServerVersion()) < pluginManifest.Automatica.MinCoreServerVersion)
                {
                    logger.LogError($"Core server version must be at least {pluginManifest.Automatica.MinCoreServerVersion}! Current version is {ServerInfo.GetServerVersion()}");
                    return false;
                }

                var pluginFiles = Directory.GetFiles(Path.Combine(tempPath, pluginManifest.Automatica.ComponentName), "*.dll");

                foreach (var dep in pluginManifest.Automatica.Dependencies)
                {
                    var found = false;

                    if (dep == pluginManifest.Automatica.Output)
                    {
                        continue;
                    }

                    foreach (var file in pluginFiles)
                    {
                        var fileInfo = new FileInfo(file);

                        if (fileInfo.Name == dep)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        logger.LogError($"Dependency {dep} is missing");
                        return false;
                    }
                }

                return true;
            }
            catch(Exception e)
            {
                logger.LogError(e, "Exception occured while checking the update package");
                return false;
            }
        }
    }
}
