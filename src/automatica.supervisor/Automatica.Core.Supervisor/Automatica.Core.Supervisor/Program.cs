using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Reflection;

namespace Automatica.Core.Supervisor
{
    class Program
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
              .AddEnvironmentVariables()
              .Build();

            var logBuild = new LoggerConfiguration()
              .WriteTo.Console()
              .WriteTo.RollingFile(Path.Combine("logs", "logs.log"), retainedFileCountLimit: 10, fileSizeLimitBytes: 1024 * 30)
              .MinimumLevel.Verbose();

            Log.Logger = logBuild.CreateLogger();
            Log.Logger.Information($"Starting Automatica.Supervisor...Version {NetStandardUtils.Version.GetAssemblyVersion()}, Datetime {DateTime.Now}");


            CreateWebHostBuilder(config["server:port"]).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string port) =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>().UseUrls($"http://*:8080/").UseSerilog();
    }
}
