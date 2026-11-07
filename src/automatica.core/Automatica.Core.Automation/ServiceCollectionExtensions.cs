using Automatica.Core.Automation.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Automatica.Core.Automation
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAutomations(this IServiceCollection services)
        {
            services.TryAddSingleton<IAutomationEnvironment, CoreAutomationEnvironment>();
            services.TryAddSingleton<IAutomationStore, EfAutomationStore>();
            services.TryAddSingleton<IAutomationEngine, AutomationEngine>();
            return services;
        }
    }
}
