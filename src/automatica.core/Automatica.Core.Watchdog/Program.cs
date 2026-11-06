using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Filters;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Watchdog
{
    static class Program
    {
        private static ILogger _logger;

        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(ServerInfo.GetConfigDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            Console.Title = "Automatica.Core.Watchdog";
            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Console()
              .MinimumLevel.Verbose()
              .Filter.ByExcluding(Matching.FromSource("Microsoft"))
              .CreateLogger();

            _logger = CoreLoggerFactory.GetLogger(config,null, "Watchdog");
            _logger.LogInformation($"Starting WatchDog...Version {ServerInfo.GetServerVersion()}, Datetime {ServerInfo.StartupTime}");

            var tz = config["TZ"] ?? config["server:TZ"];
            if (!String.IsNullOrEmpty(tz))
            {
                Environment.SetEnvironmentVariable("TZ", tz);
            }

            var fi = new FileInfo(Assembly.GetEntryAssembly().Location);
            var appName = Path.Combine(fi.DirectoryName, ServerInfo.ServerExecutable);


            //logger.LogInformation($"Getting all processes by name {ServerInfo.ServerExecutable}...");
            //var processes = Process.GetProcessesByName(ServerInfo.ServerExecutable);
            //logger.LogInformation($"Getting all processes by name {ServerInfo.ServerExecutable}...done");

            //foreach (var process in processes)
            //{
            //    logger.LogInformation($"Kill {process.ProcessName} - {process.MainWindowTitle}...");
            //    process.Kill();
            //}

            var tmpPath = ServerInfo.GetUpdateStagingDirectory();

            if (Directory.Exists(tmpPath))
            {
                Directory.Delete(tmpPath, true);
            }

            if(File.Exists(ServerInfo.GetUpdateFilePath()))
            {
                File.Delete(ServerInfo.GetUpdateFilePath());
            }

            RemoveLegacyTempUpdateFiles();

            ProcessStartInfo processInfo;
            if (!File.Exists(appName))
            {
                if (!File.Exists(appName + ".dll"))
                {
                    _logger.LogError($"Could not fine {appName} or {appName}.dll - exiting startup...");
                    return;
                }
                _logger.LogInformation($"Starting with dotnet {appName}.dll");
                processInfo = new ProcessStartInfo("dotnet", appName + ".dll");
            }
            else
            {
                processInfo = new ProcessStartInfo(appName);
            }
            
            processInfo.WorkingDirectory = Environment.CurrentDirectory;

          

            Process process = null;
            try
            {
                int restartCount = 0;
                while (true)
                {
                    _logger.LogInformation($"Starting {appName}");

                    var startTime = DateTime.Now;
                    process = Process.Start(processInfo);
                    process!.ErrorDataReceived += ProcessOnErrorDataReceived;

                    process.WaitForExit();
                    restartCount++;

                    var diffNow = DateTime.Now - startTime;
                    if (diffNow.TotalMinutes >= 30)
                    {
                        restartCount = 0;
                    }


                    process.ErrorDataReceived -= ProcessOnErrorDataReceived;
                    var exitCode = process.ExitCode;
                    _logger.LogInformation($"{appName} stopped with exit code {exitCode}");

                    if (exitCode == ServerInfo.ExitCodeRestart || exitCode == ServerInfo.ExitCodePluginUpdateInstall)
                    {
                        // requested restarts do not count for the crash loop backoff
                        restartCount = 0;
                    }

                    if (exitCode == ServerInfo.ExitCodeRestart)
                    {
                        _logger.LogInformation($"Core system requested a normal restart....");
                    }
                    else if (PrepareUpdateIfExists() || process.ExitCode == ServerInfo.ExitCodeUpdateInstallDocker)
                    {
                        Environment.Exit(2); //restart
                        return;
                    }

                    process = null;

                    Thread.Sleep(GetRestartDelay(exitCode, restartCount));
                }
            }
            catch (TaskCanceledException)
            {
                process?.Kill();
            }

            Console.ReadLine();
            //cancelToken.Cancel();
        }

        /// <summary>
        /// Crash loop backoff: an unexpected exit within 30 minutes after the start doubles the delay (500ms, 1s, 2s,... max 60s).
        /// Requested restarts are not delayed.
        /// </summary>
        internal static TimeSpan GetRestartDelay(int exitCode, int restartCount)
        {
            var defaultDelay = TimeSpan.FromMilliseconds(500);
            if (exitCode == ServerInfo.ExitCodeRestart || exitCode == ServerInfo.ExitCodePluginUpdateInstall || restartCount <= 1)
            {
                return defaultDelay;
            }

            var exponent = Math.Min(restartCount - 1, 7);
            var delay = TimeSpan.FromMilliseconds(defaultDelay.TotalMilliseconds * Math.Pow(2, exponent));
            var maxDelay = TimeSpan.FromSeconds(60);
            delay = delay > maxDelay ? maxDelay : delay;

            _logger.LogWarning($"Process exited unexpectedly {restartCount} times in a row, restarting in {delay.TotalSeconds}s");
            return delay;
        }

        /// <summary>
        /// Before 2026-11 the update was staged in the world writable temp directory, anything there is ignored and removed
        /// </summary>
        private static void RemoveLegacyTempUpdateFiles()
        {
            try
            {
                var legacyDirectory = Path.Combine(ServerInfo.GetTempPath(), ServerInfo.UpdateStagingDirectoryName);
                if (Directory.Exists(legacyDirectory))
                {
                    Directory.Delete(legacyDirectory, true);
                }

                var legacyFile = Path.Combine(ServerInfo.GetTempPath(), ServerInfo.UpdateFileName);
                if (File.Exists(legacyFile))
                {
                    File.Delete(legacyFile);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not remove the legacy update files from the temp directory");
            }
        }

        private static void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            _logger.LogError(e.Data);
        }

        private static bool PrepareUpdateIfExists()
        {
            // private staging directory (0700), the old world writable /tmp locations are never used
            var tmpPath = ServerInfo.GetUpdateStagingDirectory();
            _logger.LogInformation($"See if we have an update pending...");

            if (Directory.Exists(tmpPath))
            {
                Directory.Delete(tmpPath, true);
            }

            var updateFile = ServerInfo.GetUpdateFilePath();

            if (!File.Exists(updateFile))
            {
                _logger.LogError("no update file found...");
                return false;
            }
            _logger.LogInformation($"Update file found, prepare it...");

            try
            {
                // verifies the signature before the package is extracted
                var manifest = Common.Update.Update.GetUpdateManifest(_logger, updateFile, tmpPath);
                if (manifest == null)
                {
                    _logger.LogError("Invalid update package");
                    File.Delete(updateFile);
                    if (Directory.Exists(tmpPath))
                    {
                        Directory.Delete(tmpPath, true);
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not unpack update package");
                File.Delete(updateFile);
                if (Directory.Exists(tmpPath))
                {
                    Directory.Delete(tmpPath, true);
                }
                return false;
            }

            _logger.LogInformation($"Updated prepared, restarting to install it");

            return true;
        }
    }
}
