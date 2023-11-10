using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.EF.Backup.SqLite
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSqLiteBackup(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseBackup, SqLiteBackup>();
        }
    }
}
