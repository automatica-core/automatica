using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Internals
{
    public static class ServiceCollectionExtension
    {
        public static void AddInternals(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<INodeTemplateCache, NodeTemplateCache>();
            services.AddSingleton<INodeInstanceCache, NodeInstanceCache>();
            //services.AddSingleton<IVisualizationPageCache, VisualizationPageCache>();
            //services.AddSingleton<IVisualizationCache, VisualizationCache>();
        }
    }
}
