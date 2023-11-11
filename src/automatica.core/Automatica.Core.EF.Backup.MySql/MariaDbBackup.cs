using Automatica.Core.EF.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Automatica.Core.EF.Backup.MySql
{
    internal class MariaDbBackup : IDatabaseBackup
    {
        private readonly ILogger<MariaDbBackup> _logger;
        public DatabaseTypeEnum DbType => DatabaseTypeEnum.MariaDb;

        public MariaDbBackup(ILogger<MariaDbBackup> logger)
        {
            _logger = logger;
        }

        public async Task StartBackup(string connectionString, string targetFile, CancellationToken token = default)
        {
            try
            {
                await using var conn = new MySqlConnection(connectionString);
                await using var cmd = new MySqlCommand();
                using var mb = new MySqlBackup(cmd);
                cmd.Connection = conn;
                await conn.OpenAsync(token);
                mb.ExportToFile(targetFile);
                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on backup database");
            }

        }
    }
}