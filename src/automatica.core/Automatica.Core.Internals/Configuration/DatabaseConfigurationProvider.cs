using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        private bool _migrated;

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
                using var dbContext = new AutomaticaContext(_config);
                try
                {
                    // Only on the first load: since EF Core 9 Migrate() takes a write lock
                    // (__EFMigrationsLock) even with nothing to apply, so a Reload() while another
                    // connection holds a SQLite write transaction blocks for the full command
                    // timeout. DatabaseInit migrates at startup and reloads the config afterwards.
                    if (!_migrated)
                    {
                        dbContext.Database.Migrate();
                        _migrated = true;
                    }

                    // build the new values first and only swap them in when everything could be read,
                    // a failing read must not wipe all db:* keys
                    var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var setting in dbContext.Settings.AsNoTracking())
                    {
                        data["db:" + setting.ValueKey] = $"{setting.Value}";
                    }

                    Data = data;
                }
                catch (Exception e)
                {
                    // Automatica.Core.Logging (SystemLogger) cannot be referenced from here (circular), it logs to serilog as well
                    Serilog.Log.Logger.Error(e, "Could not load the configuration from the database, keeping the previous values");
                }
            }


        }
    }
}
