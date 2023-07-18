using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Configuration
{
    public class DatabaseConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfiguration _config;

        public DatabaseConfigurationProvider()
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(ServerInfo.GetConfigDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
                _config = builder.Build();
        }

        public override void Load()
        {
            Data.Clear();
            using var dbContext = new AutomaticaContext(_config);

            dbContext.Database.Migrate();

            foreach (var setting in dbContext.Settings)
            {
                Data.Add("db:" + setting.ValueKey, $"{setting.Value}");
            }
        }
    }
}
