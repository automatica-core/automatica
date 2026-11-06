using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Automatica.Core.EF.Models
{
    /// <summary>
    /// Creates short lived <see cref="AutomaticaContext"/> instances for singleton services. The caller owns
    /// (and has to dispose) every created context; never share one context between threads or operations.
    /// </summary>
    public class AutomaticaContextFactory : IDbContextFactory<AutomaticaContext>
    {
        private readonly IConfiguration _configuration;

        public AutomaticaContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AutomaticaContext CreateDbContext()
        {
            return new AutomaticaContext(_configuration);
        }
    }

    public static class AutomaticaContextFactoryExtensions
    {
        public static IServiceCollection AddAutomaticaContextFactory(this IServiceCollection services)
        {
            services.TryAddSingleton<IDbContextFactory<AutomaticaContext>, AutomaticaContextFactory>();
            return services;
        }
    }
}
