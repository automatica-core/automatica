using Microsoft.Extensions.DependencyInjection;
using System;

namespace Automatica.Core.Internals.Templates
{
    public class TemplateFactoryProvider<T> where T : PropertyTemplateFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public TemplateFactoryProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public T CreateInstance(Guid owner)
        {
            var factory = _serviceProvider.GetRequiredService<T>();

            factory.Owner = owner;
            factory.AllowOwnerOverride = true; //TODO: disable in 2 versions again
            return factory;
        }
    }
}
