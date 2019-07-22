using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;

namespace Automatica.Core.Base.Common
{
    /// <summary>
    /// Provides some information about the running system
    /// </summary>
    public static class ServerInfo
    {
        private static DateTime? _startupTimePrivate;
        public const string DbConfigVersionKey = "ConfigVersion";
        public const string ServerExecutable = "Automatica.Core";
        public const string SlaveExceuteable = "Automatica.Core.Plugin.Dockerize";
        public const string WatchdogExecutable = "Automatica.Core.Watchdog";
        public const string BootloderExecutable = "Automatica.Core.Bootloader";
        public const string UpdateFileName = "Automatica.Core.Update.zip";
        public const string DriversDirectory = "Drivers";
        public const string LogicsDirectory = "Rules";
        public const string PluginUpdateDirectoryName = "Automatica.Core.Plugins";

        public const int ExitCodeUpdateInstall = 2;
        public const int ExitCodePluginUpdateInstall = 3;


        public const string SelfSlaveId = "172bb906-b584-4d5d-85e8-b6d881498534";

        public static Guid SelfSlaveGuid => new Guid(SelfSlaveId);

        /// <summary>
        /// Gets the loaded config version (increment after every save)
        /// </summary>
        public static int LoadedConfigVersion { get; set; }

        /// <summary>
        /// Current config version in the database
        /// </summary>
        public static int DbConfigVersion { get; set; }

        /// <summary>
        /// The unique id of the server
        /// </summary>
        public static Guid ServerUid { get; set; }

        /// <summary>
        /// Directory where the drivers are searched
        /// </summary>
        public static string DriverDirectoy { get; set; }

        /// <summary>
        /// Driver search pattern, used for development mode
        /// </summary>
        public static string DriverPattern { get; set; } = "*.dll";

        /// <summary>
        /// Indicates if the server runs in development mode
        /// </summary>
        public static bool IsInDevelopmentMode { get; set; } = false;

        /// <summary>
        /// Returns the startup time
        /// </summary>
        public static DateTime StartupTime
        {
            get
            {
                if (!_startupTimePrivate.HasValue)
                {
                    _startupTimePrivate = DateTime.Now;
                }
                return _startupTimePrivate.Value;
            }
        }

        public static bool IsConnectedToCloud { get; set; }

        public static string Rid
        {
            get
            {
                var rid = "";
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    rid = "linux-";
                }
                else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    rid = "win-";
                }

                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.Arm:
                        rid += "arm";
                        break;
                    case Architecture.Arm64:
                        rid += "arm64";
                        break;
                    case Architecture.X64:
                        rid += "x64";
                        break;
                    case Architecture.X86:
                        rid += "x86";
                        break;
                }

                return rid;
            }
        }

        /// <summary>
        /// Gets the web port
        /// </summary>
        public static string WebPort { get; set; }


        /// <summary>
        /// Gets the Automatica.Core version
        /// </summary>
        /// <returns></returns>
        public static string GetServerVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        public static string GetBasePath()
        {
            return new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
        }

        public static string GetConfigDirectory()
        {
            var configDir = GetBasePath();

            if (Directory.Exists(Path.Combine(configDir, "config")))
            {
                configDir = Path.Combine(configDir, "config");
            }

            return configDir;
        }

        public static string GetLogDirectory()
        {
            var logDirectory = GetBasePath();
            logDirectory = Path.Combine(logDirectory, "logs");
            return logDirectory;
        }
    }
}
