using Automatica.Core.Base.Common;
using Automatica.Core.Base.OS;
using Automatica.Core.Common.Update.Signing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Automatica.Core.Common.Update
{
    public static class Update
    {
        public static bool CheckUpdateFile(ILogger logger, string fileName, string currentRid)
        {
            var tempPath = ServerInfo.CreatePrivateTempDirectory();
            var check = CheckUpdateFileInternal(logger, tempPath, fileName, currentRid);

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            return check;
        }

        /// <summary>
        /// Verifies the signature of the update package and extracts it to <paramref name="tempPath"/>
        /// </summary>
        /// <returns>the manifest, null if the package is invalid or its signature is not trusted</returns>
        public static UpdateManifest GetUpdateManifest(ILogger logger, string fileName, string tempPath)
        {
            var manifest = GetUpdateManifestInternal(logger, tempPath, fileName);
            return manifest;
        }

        /// <summary>
        /// Verifies the signature (and the anti-rollback rule) of an update package before it is extracted
        /// </summary>
        /// <returns>the signature, null if an unsigned package was accepted by the policy</returns>
        /// <exception cref="PackageVerificationException">the package must not be installed</exception>
        public static PackageSignature VerifyUpdatePackage(ILogger logger, string fileName, string installedVersion)
        {
            var result = PackageVerifier.VerifyZip(fileName, PackageSignatureFormat.TypeUpdate, PackageSignatureFormat.UpdateName);
            var signature = PackageVerifier.EnsureTrusted(result, Path.GetFileName(fileName), logger);
            PackageVerifier.EnsureNoDowngrade(signature, installedVersion, PackageSignatureFormat.UpdateName, logger);
            return signature;
        }

        /// <summary>
        /// Verifies an extracted update directory (bootloader) against the signature file inside it
        /// </summary>
        /// <exception cref="PackageVerificationException">the directory must not be installed</exception>
        public static PackageSignature VerifyExtractedUpdate(ILogger logger, string directory)
        {
            var result = PackageVerifier.VerifyDirectory(directory, PackageSignatureFormat.TypeUpdate, PackageSignatureFormat.UpdateName, "", true, true);
            return PackageVerifier.EnsureTrusted(result, directory, logger);
        }

        private static UpdateManifest GetUpdateManifestInternal(ILogger logger, string tempPath, string fileName)
        {
            PackageSignature signature;
            try
            {
                signature = VerifyUpdatePackage(logger, fileName, SafeServerVersion());
            }
            catch (PackageVerificationException e)
            {
                logger.LogError(e.Message);
                return null;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not verify the update file");
                return null;
            }

            try
            {
                ZipFile.ExtractToDirectory(fileName, tempPath, true);

                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    LinuxHelper.Bash($"chmod 755 {Path.Combine(tempPath, ServerInfo.BootloderExecutable)}");
                    LinuxHelper.Bash($"chmod 755 {Path.Combine(tempPath, ServerInfo.ServerExecutable)}");
                    LinuxHelper.Bash($"chmod 755 {Path.Combine(tempPath, ServerInfo.WatchdogExecutable)}");
                }

            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not unzip update file");
                return null;
            }

            var manifest = Path.Combine(tempPath, "automatica-update.manifest");

            if (!File.Exists(manifest))
            {
                logger.LogError("Could not find automatica-update.manifest file");
                return null;
            }

            var manifestStr = "";
            using (var reader = new StreamReader(manifest))
            {
                manifestStr = reader.ReadToEnd();
            }
            try
            {
                var updateManifest = JsonConvert.DeserializeObject<UpdateManifest>(manifestStr);

                if (signature != null && updateManifest != null)
                {
                    // the manifest is signed too, but the signed values are the ones that count
                    if (!Version.TryParse(signature.Version, out var signedVersion) || updateManifest.Version != signedVersion)
                    {
                        logger.LogError($"Update manifest version {updateManifest.Version} does not match the signed version {signature.Version}");
                        return null;
                    }
                    if (!string.IsNullOrEmpty(signature.Rid) && signature.Rid != updateManifest.Rid)
                    {
                        logger.LogError($"Update manifest rid {updateManifest.Rid} does not match the signed rid {signature.Rid}");
                        return null;
                    }
                }

                return updateManifest;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception occured while checking the update package");
                return null;
            }
        }

        private static string SafeServerVersion()
        {
            try
            {
                return ServerInfo.GetServerVersion();
            }
            catch
            {
                return null;
            }
        }

        private static bool CheckUpdateFileInternal(ILogger logger, string tempPath, string fileName, string currentRid)
        {
            try
            {
                var updateManifest = GetUpdateManifestInternal(logger, tempPath, fileName);

                if(updateManifest == null)
                {
                    return false;
                }

                if (updateManifest.Rid != currentRid)
                {
                    logger.LogError($"Invalid RID detected (found: {updateManifest.Rid}, expected: {currentRid}");
                    return false;
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
