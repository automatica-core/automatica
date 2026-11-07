using Automatica.Core.Shading.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Automatica.Core.Shading
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddShading(this IServiceCollection services)
        {
            services.TryAddSingleton<IShadingEnvironment, CoreShadingEnvironment>();
            services.TryAddSingleton<IShadingStore, EfShadingStore>();
            services.TryAddSingleton<IShadingEngine, ShadingEngine>();
            return services;
        }
    }
}
