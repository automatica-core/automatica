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

        /// <summary>
        /// Time until the next local midnight, always positive.
        /// </summary>
        private static TimeSpan CalculateDiffToMidnight()
        {
            var now = DateTimeHelper.ProviderInstance.GetLocalNow().DateTime;
            var nextMidnight = now.Date.AddDays(1);

            var diff = nextMidnight - now;
            return diff < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : diff;
        }

        private string GetBackupFile()
        {
            return Path.Combine(ServerInfo.GetBackupDirectory(), $"{DatabaseType}-{DateTimeHelper.ProviderInstance.GetLocalNow().DateTime:yyyy-MM-dd}.bak");
        }

        /// <summary>
        /// One shot timer, re-armed after every run: the backup runs once a day at midnight (and once shortly after the
        /// start if there is no backup of today yet), independent of when the server was started.
        /// </summary>
        private void ScheduleNextBackup(TimeSpan dueTime)
        {
            try
            {
                _backupTimer?.Change(dueTime, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // stopped
            }
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
                    // backups contain user hashes and (encrypted) secrets: owner only
                    ServerInfo.EnsurePrivateDirectory(ServerInfo.GetBackupDirectory());
                    var backupFile = GetBackupFile();
                    await _backupService.StartBackup(
                        ConnectionStringHelper.GetConnectionString(_config, _logger).connectionString,
                        backupFile);
                    if (File.Exists(backupFile))
                    {
                        ServerInfo.MakeFilePrivate(backupFile);
                    }
                }

                await ClearOldBackups();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not create database backup {e}");
            }
            finally
            {
                ScheduleNextBackup(CalculateDiffToMidnight());
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

                // a backup of today already exists: the next one at midnight, otherwise one shortly after the start
                // (not during the start, it must not slow it down)
                var delay = CalculateDiffToMidnight();
                try
                {
                    if (!File.Exists(GetBackupFile()))
                    {
                        delay = TimeSpan.FromMinutes(5);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not check for an existing backup {ex}");
                }

                _backupTimer = new Timer(BackupCallback, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                ScheduleNextBackup(delay);
                try
                {
                    await ClearOldBackups();
                }
                catch (Exception ex)
                {
                    // e.g. the backup directory could not be created - must not fail the host start
                    _logger.LogError(ex, $"Could not clear old backups {ex}");
                }
                _logger.LogInformation($"Backup service started for {databaseType} database type, first backup will be in {delay}");
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