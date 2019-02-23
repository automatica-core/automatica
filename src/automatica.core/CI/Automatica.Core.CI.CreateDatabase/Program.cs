
using Automatica.Core.Base.Visu;
using Automatica.Core.EF.Helper;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.Database;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Reflection;

namespace Automatica.Core.CI.CreateDatabase
{
    static class Program
    {
        static void Main(string[] args)
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
            
            var webHost = BuildWebHost(args);

            string loadDirectory = "";

            if (args.Length > 0)
            {
                loadDirectory  = args[0];
            }
            else
            {
                loadDirectory = fi.DirectoryName;
            }

            DatabaseInit.EnusreDatabaseCreated(webHost.Services);

            var server = new CoreServer(webHost.Services);
            server.Load(loadDirectory, "*.dll");


            Console.WriteLine($"Done....");
            Environment.Exit(0);
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls($"http://*:9999/")
                .UseSerilog();

            return webHost.Build();
        }
    }
}
