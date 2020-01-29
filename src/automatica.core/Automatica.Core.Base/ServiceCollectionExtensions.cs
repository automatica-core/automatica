using Automatica.Core.Base.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Base
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDispatcher(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDispatcher, Dispatcher>();
        }
    }
}
