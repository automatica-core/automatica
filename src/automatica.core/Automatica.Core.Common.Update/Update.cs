using Automatica.Core.Base.Common;
using Automatica.Core.Base.OS;
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
            var tempPath = Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString());
            var check = CheckUpdateFileInternal(logger, tempPath, fileName, currentRid);

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            
            return check;
        }

        public static UpdateManifest GetUpdateManifest(ILogger logger, string fileName, string tempPath)
        {
            var manifest = GetUpdateManifestInternal(logger, tempPath, fileName);
            return manifest;
        }

        private static UpdateManifest GetUpdateManifestInternal(ILogger logger, string tempPath, string fileName)
        {
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

            var files = Directory.GetFiles(tempPath, "automatica-update.manifest");

            if (files.Length == 0)
            {
                logger.LogError("Could not find automatica-update.manifest file");
                return null;
            }

            var manifest = files[0];

            var manifestStr = "";
            using (var reader = new StreamReader(manifest))
            {
                manifestStr = reader.ReadToEnd();
            }
            try
            {
                var updateManifest = JsonConvert.DeserializeObject<UpdateManifest>(manifestStr);

                return updateManifest;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception occured while checking the update package");
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
