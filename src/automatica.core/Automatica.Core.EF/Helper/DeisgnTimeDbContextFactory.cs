using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Automatica.Core.EF.Helper
{
    internal class DeisgnTimeDbContextFactory : IDesignTimeDbContextFactory<AutomaticaContext>
    {
        public AutomaticaContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .Build();

            return new AutomaticaContext(configuration);
        }
    }
}
