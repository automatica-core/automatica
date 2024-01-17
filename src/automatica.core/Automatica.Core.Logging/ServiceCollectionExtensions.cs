using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logging
{
    public static class ServiceCollectionExtensions
    {
        public static void AddCoreLogger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ICoreLoggerSettings, CoreLoggerSettings>();

            services.Replace(ServiceDescriptor.Singleton(typeof(ILogger), typeof(CoreLogger)));
            services.Replace(ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(CoreLoggerFactory)));

        }
    }
}
