using System;
using System.IO;
using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Discovery;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Versioning;
using Automatica.Core.Runtime.Database;
using Serilog;
using Automatica.Core.Internals;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Automatica.Core.Logging;

namespace Automatica.Core
{
    static class Program
    {
        static void Main(string[] args)
        {
           
            var config = new ConfigurationBuilder()
                .SetBasePath(ServerInfo.GetConfigDirectory())
                .AddJsonFile(ServerInfo.GetConfigFileName(), false)
                .AddEnvironmentVariables()
                .Build();

            var logBuild = LogConfiguration.ConfigureLogger();

            Log.Logger = logBuild.CreateLogger();

            if (Environment.GetEnvironmentVariable("PRINT_ENV") == "true")
            {
                foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    var envVar = $"{env.Key}={env.Value}";
                    Log.Logger.Debug($"Using env variable: {envVar}");
                }
            }

            var logger = SystemLogger.Instance;

            if(!File.Exists(Path.Combine(ServerInfo.GetConfigDirectory(), "appsettings.json"))) {
                logger.LogError($"Could not find appsettings.json in {ServerInfo.GetConfigDirectory()}");
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MQTT_LOG_VERBOSE")))
            {
                //MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                //{
                //    var trace =
                //        $"mqtt >> [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                //    if (e.TraceMessage.Exception != null)
                //    {
                //        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                //    }

                //    logger.LogDebug(trace);
                //};
            }

            var webHost = BuildWebHost(config["server:port"], config["server:ssl_port"]);

            if (args.Length > 0 && args[0] == "develop")
            {
                ServerInfo.PluginDirectory = args[1];
                ServerInfo.PluginFilePattern= args[2];
                ServerInfo.IsInDevelopmentMode = true;
            }
            else
            {
                var pluginDir = Environment.GetEnvironmentVariable("AUTOMATICA_PLUGIN_DIR");
                if (!String.IsNullOrEmpty(pluginDir))
                {
                    ServerInfo.PluginDirectory = pluginDir;
                }
                else
                {
                    ServerInfo.PluginDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                }
            }

            logger.LogInformation($"Starting...Version {ServerInfo.GetServerVersion()}, Datetime {ServerInfo.StartupTime}. Running .NET Core Version {GetNetCoreVersion()}");

            if (ServerInfo.InDocker)
            {
                logger.LogInformation($"Running in docker mode...");
            }

            var db = webHost.Services.GetRequiredService<AutomaticaContext>();
            DatabaseInit.EnsureDatabaseCreated(webHost.Services);

          

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

        public static IWebHost BuildWebHost(string port, string sslPort)
        {
            ServerInfo.WebPort = port;
            ServerInfo.SslWebPort = sslPort; 

            var configDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;

            var webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseKestrel(o => {

                    if (!String.IsNullOrWhiteSpace(port))
                    {
                        o.ListenAnyIP(Convert.ToInt32(port));
                    }

                    if (!String.IsNullOrWhiteSpace(sslPort))
                    {
                        o.ListenAnyIP(Convert.ToInt32(sslPort), options =>
                        {
                            options.UseHttps(a =>
                            {
                                var x509Cert = new X509Certificate2(Path.Combine(configDir, "cert/certificate.pfx"),
                                    "local");
                                a.ServerCertificate = x509Cert;
                            });
                        });
                    }
                })
                .ConfigureAppConfiguration(a =>
                {
                    if (Directory.Exists(Path.Combine(configDir, "config")))
                    {
                        configDir = Path.Combine(configDir, "config");
                    }

                    a.SetBasePath(configDir);
                    a.AddJsonFile(ServerInfo.GetConfigFileName(), true);
                    a.AddDatabaseConfiguration();
                    a.AddEnvironmentVariables();
                })
                //.UseElectron(new string[])
                .UseSerilog()
                .ConfigureLogging(logging => {
                    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Trace);
                    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Trace);
                }).ConfigureAppConfiguration((AutomaticaContext, config) => {
                    config
                        .SetBasePath(ServerInfo.GetConfigDirectory())
                        .AddJsonFile(ServerInfo.GetConfigFileName(), false)
                        .AddEnvironmentVariables()
                        .AddDatabaseConfiguration();
                });

            //if (HybridSupport.IsElectronActive)
            //{
            //    ServerInfo.WebPort = "80";
            //}

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
