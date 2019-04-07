using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Slave
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
              .SetBasePath(new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName)
              .AddJsonFile("appsettings.json", true)
              .Build();

            CreateWebHostBuilder(config["server:port"]).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string port) =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>().UseUrls($"https://*:{port}/");
    }
}
