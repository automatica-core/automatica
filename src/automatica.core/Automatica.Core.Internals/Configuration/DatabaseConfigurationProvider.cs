using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Configuration
{
    public class DatabaseConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfiguration _config;
        private readonly object _lock = new object();

        public DatabaseConfigurationProvider()
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(ServerInfo.GetConfigDirectory())
                .AddJsonFile(ServerInfo.GetConfigFileName())
                .AddEnvironmentVariables();
                _config = builder.Build();
        }

        public override void Load()
        {
            lock (_lock)
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
}
