using System;
using System.IO;
using System.Linq;
using Automatica.Core.Base.Common;

using Automatica.Core.EF.Models;
using Automatica.Discovery;
using ElectronNET.API;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Versioning;
using Automatica.Core.Runtime.Database;
using Serilog;
using Serilog.Filters;
using Automatica.Core.Internals;
using MessagePack;

namespace Automatica.Core
{
    static class Program
    {
        static void Main(string[] args)
        {


            var config = new ConfigurationBuilder()
                .SetBasePath(new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName)
                .AddJsonFile("appsettings.json", true)
                .Build();

            var logBuild = new LoggerConfiguration()
              .WriteTo.Console()
              .WriteTo.RollingFile(Path.Combine("logs", "dotnet.log"), retainedFileCountLimit: 10, fileSizeLimitBytes: 1024 * 30)
              .MinimumLevel.Verbose();

            Log.Logger = logBuild.CreateLogger();

            var webHost = BuildWebHost(config["server:port"]);

            if (args.Length > 0 && args[0] == "develop")
            {
                ServerInfo.DriverDirectoy = args[1];
                ServerInfo.DriverPattern= args[2];
                ServerInfo.IsInDevelopmentMode = true;
            }
            else
            {
                ServerInfo.DriverDirectoy = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            }
            var logger = SystemLogger.Instance;
            logger.LogInformation($"Starting...Version {ServerInfo.GetServerVersion()}, Datetime {ServerInfo.StartupTime}. Running .NET Core Version {GetNetCoreVersion()}");

            var db = webHost.Services.GetRequiredService<AutomaticaContext>();
            DatabaseInit.EnusreDatabaseCreated(webHost.Services);

            var serverId = db.Settings.SingleOrDefault(a => a.ValueKey == "ServerUID");

            if (serverId == null)
            {
                var guid = Guid.NewGuid();
                db.Settings.Add(new Setting
                {
                    ValueKey = "ServerUID",
                    Value = guid,
                    Group = "SERVER.SETTINGS",
                    IsVisible = false,
                    Type = (long)PropertyTemplateType.Text
                });
                ServerInfo.ServerUid = guid;
                db.SaveChanges();
            }
            else
            {
                ServerInfo.ServerUid = new Guid(serverId.ValueText);
            }

            webHost.Services.GetService<DiscoveryService>();
            webHost.Run();

            logger.LogInformation("Stopped...");
        }

        public static IWebHost BuildWebHost(string port)
        {
            ServerInfo.WebPort = port;

            var webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                //.UseElectron(new string[])
                .UseUrls($"http://*:{port}/")
                .UseSerilog()
                .ConfigureLogging(logging => {
                    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Trace);
                    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Trace);
                });

            if (HybridSupport.IsElectronActive)
            {
                ServerInfo.WebPort = "80";
            }

            return webHost.Build();
        }

        public static string GetNetCoreVersion()
        {
            var framework = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName;
            return framework;
        }
    }
}
