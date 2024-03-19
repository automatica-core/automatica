using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Notification
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationManager(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<INotificationManager, NotificationManager>();

            return serviceCollection;
        }
    }
}
