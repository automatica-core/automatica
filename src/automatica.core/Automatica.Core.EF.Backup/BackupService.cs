using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Configuration;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.EF.Backup
{
    public class BackupService : IHostedService
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackupService> _logger;
        private Timer? _backupTimer;
        private IDatabaseBackup? _backupService;

        private const int BackupRetentionDays = 14;

        public BackupService(IConfiguration config, IServiceProvider serviceProvider, ILogger<BackupService> logger)
        {
            _config = config;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private static int CalculateDiffToMidnight()
        {
            var now = DateTimeHelper.ProviderInstance.GetLocalNow().DateTime;

            var midnight = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 0);

            return Convert.ToInt32((midnight - now).TotalMilliseconds);
        }

        // the database type is only known to a context after OnConfiguring ran, read it from the configuration instead
        private DatabaseTypeEnum DatabaseType => ConnectionStringHelper.GetConnectionString(_config, _logger).dbType;

        private async void BackupCallback(object? state)
        {
            // timer callback: an exception here would crash the process
            try
            {
                if (_backupService != null)
                {
                    Directory.CreateDirectory(ServerInfo.GetBackupDirectory());
                    await _backupService.StartBackup(
                        ConnectionStringHelper.GetConnectionString(_config, _logger).connectionString,
                        Path.Combine(ServerInfo.GetBackupDirectory(), $"{DatabaseType}-{DateTimeHelper.ProviderInstance.GetLocalNow().DateTime:yyyy-MM-dd}.bak"));
                }

                await ClearOldBackups();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not create database backup {e}");
            }
        }

        private async Task ClearOldBackups()
        {
            await Task.CompletedTask;
            var files = Directory.GetFiles(ServerInfo.GetBackupDirectory(), $"{DatabaseType}-*");
            var sortedFiles = files.OrderBy(a => a).ToList();

            if (sortedFiles.Count < BackupRetentionDays)
            {
                return;
            }

            var filesToDelete = sortedFiles.Take(sortedFiles.Count - BackupRetentionDays).ToList();
            foreach (var file in filesToDelete)
            {
                File.Delete(file);
                _logger.LogInformation($"Delete old db backup {file}");
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var backupServices = _serviceProvider.GetServices<IDatabaseBackup>().ToList();

            try
            {
                Directory.CreateDirectory(ServerInfo.GetBackupDirectory());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not create backup directory {ex}");
            }


            var databaseType = DatabaseType;
            if (backupServices.Any(a => a.DbType == databaseType))
            {
                _backupService = backupServices.First(a => a.DbType == databaseType);
                var delay = CalculateDiffToMidnight();
                _backupTimer = new Timer(BackupCallback, this, 0, delay);
                try
                {
                    await ClearOldBackups();
                }
                catch (Exception ex)
                {
                    // e.g. the backup directory could not be created - must not fail the host start
                    _logger.LogError(ex, $"Could not clear old backups {ex}");
                }
                _logger.LogInformation($"Backup service started for {databaseType} database type, first backup will be in {delay}ms");
            }
            else
            {
                _logger.LogInformation($"Could not find any backup service....");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_backupTimer != null)
            {
                await _backupTimer.DisposeAsync();
            }
        }
    }
}