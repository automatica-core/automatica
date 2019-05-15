using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;
using System.Reflection;

namespace Automatica.Core.Supervisor
{
    class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
              .SetBasePath(new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName)
              .AddJsonFile("appsettings.json", true)
              .AddEnvironmentVariables()
              .Build();


            var logBuild = new LoggerConfiguration()
              .WriteTo.Console()
              .WriteTo.RollingFile(Path.Combine("logs", "logs.log"), retainedFileCountLimit: 10, fileSizeLimitBytes: 1024 * 30)
              .MinimumLevel.Verbose();

            Log.Logger = logBuild.CreateLogger();

            CreateWebHostBuilder(config["server:port"]).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string port) =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>().UseUrls($"https://*:{port}/").UseSerilog();
    }
}
