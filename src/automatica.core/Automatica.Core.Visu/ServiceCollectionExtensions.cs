using Automatica.Core.Internals.Cache.Visualization;
using Automatica.Core.Visu.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Visu
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutomaticaVisualization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IVisualizationInstanceCache, VisualizationInstanceCache>();
            services.AddSingleton<IVisualizationTemplateCache, VisualizationTemplateCache>();
            services.AddSingleton<IVisualizationPageCache, VisualizationPageCache>();
            services.AddSingleton<IVisualizationCache, VisualizationCache>();
        }
    }
}
