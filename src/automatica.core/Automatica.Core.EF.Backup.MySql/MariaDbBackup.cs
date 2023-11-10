using Automatica.Core.EF.Configuration;
using MySqlConnector;

namespace Automatica.Core.EF.Backup.MySql
{
    internal class MariaDbBackup : IDatabaseBackup
    {
        public DatabaseTypeEnum DbType => DatabaseTypeEnum.MariaDb;


        public async Task StartBackup(string connectionString, string targetFile, CancellationToken token = default)
        {
            await using var conn = new MySqlConnection(connectionString);
            await using var cmd = new MySqlCommand();
            using var mb = new MySqlBackup(cmd);
            cmd.Connection = conn;
            await conn.OpenAsync(token);
            mb.ExportToFile(targetFile);
            await conn.CloseAsync();
        }
    }
}