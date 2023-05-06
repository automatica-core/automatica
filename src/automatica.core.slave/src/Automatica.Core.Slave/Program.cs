using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Automatica.Core.Slave
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;

            if (Directory.Exists(Path.Combine(configDir, "config")))
            {
                configDir = Path.Combine(configDir, "config");
            }

            var config = new ConfigurationBuilder()
              .SetBasePath(configDir)
              .AddJsonFile("appsettings.json", true)
              .Build();

            var logBuild = new LoggerConfiguration()
             .WriteTo.Console()
             .WriteTo.RollingFile(Path.Combine("logs", "logs.log"), retainedFileCountLimit: 10, fileSizeLimitBytes: 1024 * 1000)
             .MinimumLevel.Verbose();

            Log.Logger = logBuild.CreateLogger();
            Log.Logger.Information($"Starting Automatica.Slave...Version {NetStandardUtils.Version.GetAssemblyVersion()}, Datetime {DateTime.Now}");

            CreateWebHostBuilder(config["server:port"]).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string port) 
        {
            return WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration(a =>
                {
                    var configDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
                    if (Directory.Exists(Path.Combine(configDir, "config")))
                    {
                        configDir = Path.Combine(configDir, "config");
                    }

                    a.SetBasePath(configDir);
                    a.AddEnvironmentVariables();
                    a.AddJsonFile("appsettings.json", true);
                })
                .UseStartup<Startup>().UseUrls($"http://*:{port}/").UseSerilog();
        }
    }
}

