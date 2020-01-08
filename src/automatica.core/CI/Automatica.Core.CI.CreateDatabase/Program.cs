using Automatica.Core.EF.Helper;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.Database;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.CI.CreateDatabase
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Logger.Information("Starting Automatica.Core.CI database tool");

            var config = new ConfigurationBuilder()
                .SetBasePath(new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName)
                .AddJsonFile("appsettings.json", true)
                .Build();

            var fi = new FileInfo(Assembly.GetEntryAssembly().Location);
            if(File.Exists(Path.Combine(fi.DirectoryName, DatabaseConstants.DatabaseInitName)))
            {
                File.Delete(Path.Combine(fi.DirectoryName, DatabaseConstants.DatabaseInitName));
            }

            var hostBuilder = new HostBuilder().ConfigureServices((hostContext, services) =>
                {
                    var startup = new Startup(config);
                    startup.ConfigureServices(services);
                }
            ).Build();

         
            string loadDirectory = "";

            if (args.Length > 0)
            {
                loadDirectory  = args[0];
            }
            else
            {
                loadDirectory = fi.DirectoryName;
            }

            DatabaseInit.EnusreDatabaseCreated(hostBuilder.Services);

            var server = new CoreServer(hostBuilder.Services);
            await server.Load(loadDirectory, "*.dll");


            Console.WriteLine($"Done....");
            Environment.Exit(0);
        }
    }
}
