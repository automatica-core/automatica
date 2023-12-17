using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Control
{
    public static class ServiceCollectionExtension
    {
      
        public static void AddControlContext(this IServiceCollection services)
        {
           services.AddSingleton<IControlContext, ControlContext>();
        }
    }
}
