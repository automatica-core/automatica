using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    internal static class ServiceCollectionExtension
    {
        public static void AddFrpServices(this IServiceCollection services,
            Action<FrpcOptions> settings)
        {
            services.AddFrpServicesInternal();

            var optionsBuilder = services.AddOptions<FrpcOptions>();
            optionsBuilder.Configure(settings);
        }

        public static void AddFrpServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddFrpServicesInternal();

            services.Configure<FrpcOptions>(configuration);
        }

        private static void AddFrpServicesInternal(this IServiceCollection services)
        {
            services.AddSingleton<IFrpcApiClient, FrpcApiClient>();
            services.AddSingleton<IFrpProcess, FrpProcess>();
            services.AddSingleton<IFrpService, FrpService>();
        }
    }
}
