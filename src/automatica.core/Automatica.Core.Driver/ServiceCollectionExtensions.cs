using Automatica.Core.Driver.Loader;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Driver
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutomaticaDrivers(this IServiceCollection services)
        {
            services.AddSingleton<IDriverNodesStore, DriverNodesStore>();
            services.AddSingleton<IDriverStore, DriverStore>();

            services.AddSingleton<IDriverFactoryLoader, DriverFactoryLoader>();
        }
    }
}
