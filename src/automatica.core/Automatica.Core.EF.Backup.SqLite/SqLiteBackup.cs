using Automatica.Core.EF.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.EF.Backup.SqLite
{
    internal class SqLiteBackup : IDatabaseBackup
    {
        private readonly ILogger<SqLiteBackup> _logger;
        public DatabaseTypeEnum DbType => DatabaseTypeEnum.SqLite;

        public SqLiteBackup(ILogger<SqLiteBackup> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// A consistent copy of the live database (WAL content included) with <c>VACUUM INTO</c>, a file copy of a
        /// database that is written to can be torn.
        /// </summary>
        public async Task StartBackup(string connectionString, string targetFile, CancellationToken token = default)
        {
            var tmpFile = targetFile + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetFile))!);

                // VACUUM INTO fails if the target exists
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                }

                var builder = new SqliteConnectionStringBuilder(connectionString)
                {
                    // never create an empty database if the file is missing, no pooled connection is kept open
                    Mode = SqliteOpenMode.ReadWrite,
                    Pooling = false
                };

                await using (var connection = new SqliteConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync(token);
                    await using var command = connection.CreateCommand();
                    command.CommandText = "VACUUM INTO $target";
                    command.Parameters.AddWithValue("$target", Path.GetFullPath(tmpFile));
                    await command.ExecuteNonQueryAsync(token);
                }

                File.Move(tmpFile, targetFile, true);
                _logger.LogInformation($"Created database backup {targetFile}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on backup database");
                try
                {
                    if (File.Exists(tmpFile))
                    {
                        File.Delete(tmpFile);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
    }
}