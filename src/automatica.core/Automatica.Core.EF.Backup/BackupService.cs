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
        private readonly AutomaticaContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackupService> _logger;
        private Timer? _backupTimer;
        private IDatabaseBackup? _backupService;

        private const int BackupRetentionDays = 14;

        public BackupService(IConfiguration config, AutomaticaContext context, IServiceProvider serviceProvider, ILogger<BackupService> logger)
        {
            _config = config;
            _context = context;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private static int CalculateDiffToMidnight()
        {
            var now = DateTime.Now;

            var midnight = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 0);

            return Convert.ToInt32((midnight - now).TotalMilliseconds);
        }

        private async void BackupCallback(object? state)
        {
            if (_backupService != null)
            {
                await _backupService.StartBackup(
                    ConnectionStringHelper.GetConnectionString(_config, _logger).connectionString,
                    Path.Combine("bak",$"{_context.DatabaseType}-{DateTime.Now:yyyy-MM-dd}.bak"));
            }

            await ClearOldBackups();
        }

        private async Task ClearOldBackups()
        {
            await Task.CompletedTask;
            var files = Directory.GetFiles($"bak", $"{_context.DatabaseType}-*");
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
                Directory.CreateDirectory("bak");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not create backup direcotry {ex}");
            }


            if (backupServices.Any(a => a.DbType == _context.DatabaseType))
            {
                _backupService = backupServices.First(a => a.DbType == _context.DatabaseType);
                var delay = CalculateDiffToMidnight();
                _backupTimer = new Timer(BackupCallback, this, 0, delay);
                await ClearOldBackups();
                _logger.LogInformation($"Backup service started for {_context.DatabaseType} database type, first backup will be in {delay}ms");
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