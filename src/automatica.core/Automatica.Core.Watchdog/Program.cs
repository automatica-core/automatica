using Automatica.Core.Base.Common;
using Automatica.Core.Internals;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Filters;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Watchdog
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Automatica.Core.Watchdog";
            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Console()
              .MinimumLevel.Verbose()
              .Filter.ByExcluding(Matching.FromSource("Microsoft"))
              .CreateLogger();

            var logger = SystemLogger.Instance;
            logger.LogInformation($"Starting WatchDog...Version {ServerInfo.GetServerVersion()}, Datetime {ServerInfo.StartupTime}");

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

            var tmpPath = Path.Combine(ServerInfo.GetTempPath(), $"Automatica.Core.Update");

            if (Directory.Exists(tmpPath))
            {
                Directory.Delete(tmpPath, true);
            }

            if(File.Exists(Path.Combine(ServerInfo.GetTempPath(), ServerInfo.UpdateFileName)))
            {
                File.Delete(Path.Combine(ServerInfo.GetTempPath(), ServerInfo.UpdateFileName));
            }

            ProcessStartInfo processInfo;
            if (!File.Exists(appName))
            {
                if (!File.Exists(appName + ".dll"))
                {
                    logger.LogError($"Could not fine {appName} or {appName}.dll - exiting startup...");
                    return;
                }
                logger.LogInformation($"Starting with dotnet {appName}.dll");
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

                while (true)
                {

                    logger.LogInformation($"Starting {appName}");
                    
                    process = Process.Start(processInfo);

                    process.WaitForExit();

                    var exitCode = process.ExitCode;

                    if (exitCode == ServerInfo.ExitCodeUpdateInstall)
                    {
                        if (Directory.Exists(tmpPath))
                        {
                            Directory.Delete(tmpPath, true);
                        }

                        var updateFile = Path.Combine(ServerInfo.GetTempPath(), ServerInfo.UpdateFileName);

                        if (!File.Exists(updateFile))
                        {
                            logger.LogError("no update file found...");
                            continue;
                        }

                        try
                        {
                            var manifest = Common.Update.Update.GetUpdateManifest(logger, updateFile, tmpPath);
                            if (manifest == null)
                            {
                                logger.LogError("Invalid update package");
                                File.Delete(updateFile);
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Could not unpack update package");
                            File.Delete(updateFile);
                            continue;
                        }

                        Environment.Exit(2); //restart
                        return;
                    }

                    process = null;

                    Thread.Sleep(500);
                }
            }
            catch (TaskCanceledException)
            {
                process?.Kill();
            }

            Console.ReadLine();
            //cancelToken.Cancel();
        }
    }
}
