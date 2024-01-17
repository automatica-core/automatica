using System;

namespace Automatica.Core.Internals.Templates
{
    public abstract class TemplateFactoryProvider<T> where T : PropertyTemplateFactory
    {
        private readonly IServiceProvider _serviceProvider;

        protected TemplateFactoryProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        protected abstract T CreateFactory(Guid owner, IServiceProvider serviceProvider);
        
        public T CreateInstance(Guid owner)
        {
            var factory = CreateFactory(owner, _serviceProvider);
            factory.Owner = owner;
            factory.AllowOwnerOverride = true; //TODO: disable in 2 versions again
            return factory;
        }
    }
}
