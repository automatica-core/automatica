using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.IO.Remanent;
using Automatica.Core.Base.Retry;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Automatica.Core.Base
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRetryHandler(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IRetryContext, RetryContext>();

            serviceCollection.AddResiliencePipeline("start-pipeline", builder =>
            {
                builder.AddRetry(new RetryStrategyOptions
                {
                    BackoffType = DelayBackoffType.Constant,
                    Delay = TimeSpan.FromSeconds(5),
                    MaxDelay = TimeSpan.FromSeconds(20),
                    MaxRetryAttempts = 3
                });
            });
        }
        
        public static void AddDispatcher(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDispatcher, Dispatcher>();
            serviceCollection.AddSingleton<IRemanentHandler, FileRemanentHandler>();

            AddRetryHandler(serviceCollection);

        }
    }
}
