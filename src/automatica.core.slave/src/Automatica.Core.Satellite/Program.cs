using System;
using System.IO;
using System.Reflection;
using Automatica.Core.Satellite.Abstraction.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Automatica.Core.Satellite
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
              .AddJsonFile("appsettings.json", true).Add(
                  (Action<WritableJsonConfigurationSource>)(s =>
                  {
                      s.FileProvider = null;
                      s.Path = "appsettings.json";
                      s.Optional = false;
                      s.ReloadOnChange = true;
                      s.ResolveFileProvider();
                  }))
              .Build();

            var logBuild = new LoggerConfiguration()
             .WriteTo.Console()
             .WriteTo.File(Path.Combine("logs", "logs.log"), retainedFileCountLimit: 10, fileSizeLimitBytes: 1024 * 1000)
             .MinimumLevel.Verbose();

            Log.Logger = logBuild.CreateLogger();
            Log.Logger.Information($"Starting Automatica.Slave...Version {NetStandardUtils.Version.GetAssemblyVersion()}, Datetime {DateTime.Now}");

            CreateWebHostBuilder(config["server:port"], args).Run();
        }

        public static WebApplication CreateWebHostBuilder(string port, string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
            if (Directory.Exists(Path.Combine(configDir, "config")))
            {
                configDir = Path.Combine(configDir, "config");
            }

            builder.Configuration.SetBasePath(configDir);
            builder.Configuration.AddEnvironmentVariables();
            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Configuration.Add(
                (Action<WritableJsonConfigurationSource>)(s =>
                {
                    s.FileProvider = null;
                    s.Path = "appsettings.json";
                    s.Optional = false;
                    s.ReloadOnChange = true;
                    s.ResolveFileProvider();
                }));


            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                var localPort = builder.Configuration["localPort"];
                var webPort = String.IsNullOrEmpty(localPort)
                    ? 5005
                    : Convert.ToInt32(builder.Configuration["localPort"]);
                serverOptions.ListenAnyIP(webPort);
            });

            Startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            Startup.Configure(app);

            
            return app;
        }
    }
}

