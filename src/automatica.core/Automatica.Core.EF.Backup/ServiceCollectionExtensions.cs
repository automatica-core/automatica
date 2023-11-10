using Automatica.Core.EF.Backup.MySql;
using Automatica.Core.EF.Backup.SqLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.EF.Backup
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDatabaseBackup(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, BackupService>();
            services.AddMariaDbBackup();
            services.AddSqLiteBackup();
        }
    }
}
