using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.EF.Backup.MySql
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMariaDbBackup(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseBackup, EF.Backup.MySql.MariaDbBackup>();
        }
    }
}
