using Automatica.Push.Concurrency;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Push
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutomaticaPushServices(this IServiceCollection services, IConfiguration configuration,
            bool isElectronActive)
        {
            var factory = new HubSemaphoreFactory();
            services.AddSingleton<IHubSemaphoreFactory>(factory);
        }
    }
}
