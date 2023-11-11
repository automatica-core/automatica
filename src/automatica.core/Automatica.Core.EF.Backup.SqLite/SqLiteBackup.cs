using Automatica.Core.EF.Configuration;
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
        public Task StartBackup(string connectionString, string targetFile, CancellationToken token = default)
        {
            var file = connectionString.Replace("Data Source=", "");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                File.Copy(file, targetFile, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on backup database");
            }

            return Task.CompletedTask;
        }
    }
}