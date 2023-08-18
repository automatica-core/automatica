using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.HyperSeries 
{
    public static class ServiceCollectionExtensions
    {
        public static void AddHyperSeries(this IServiceCollection services)
        {
            services.AddDbContext<HyperSeriesContext>();
        }
    }
}
