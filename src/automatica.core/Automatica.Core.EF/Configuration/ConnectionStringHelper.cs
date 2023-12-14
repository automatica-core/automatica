using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.EF.Configuration
{
    public static class ConnectionStringHelper
    {
        public static (DatabaseTypeEnum dbType, string connectionString) GetConnectionString(IConfiguration config, ILogger loggerInstance)
        {
            var envDbType = config["DATABASE_TYPE"];
            var dbType = config.GetConnectionString("AutomaticaDatabaseType");
            string useDbType = envDbType;

            if (string.IsNullOrEmpty(envDbType))
            {
                useDbType = dbType;
            }

            if (string.IsNullOrEmpty(dbType))
            {
                loggerInstance.LogError($"No DatabaseType is set! Using sqlite database driver!");
                useDbType = "sqlite";
                dbType = useDbType;
            }

            switch (useDbType.ToLower())
            {
                case DatabaseType.SqLite:
                    return (DatabaseTypeEnum.SqLite, config.GetConnectionString("AutomaticaDatabaseSqlite"));

                case DatabaseType.MariaDb:
                    var mariaDbConString = $"Server={config["MARIADB_HOST"]};User Id={config["MARIADB_USER"]};Password={config["MARIADB_PASSWORD"]};Database={config["MARIADB_DATABASE"]}";

                    if (string.IsNullOrEmpty(config["MARIADB_HOST"]))
                    {
                        mariaDbConString = config.GetConnectionString($"AutomaticaDatabaseMaria");
                    }

                    return (DatabaseTypeEnum.MariaDb, mariaDbConString);

                case DatabaseType.Memory:
                    return (DatabaseTypeEnum.Memory, "");
                default:
                    loggerInstance.LogCritical($"No or invalid database provider configured {dbType.ToLower()}\nSupported are sqlite, mariadb, memory");
                    throw new NotImplementedException($"No or invalid database provider configured {dbType.ToLower()}\nSupported are sqlite, mariadb, memory");
            }
        }
    }
}
