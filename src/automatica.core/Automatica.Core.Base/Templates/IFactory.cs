using System;

namespace Automatica.Core.Base.Templates
{
    public interface IFactory
    {
        Guid FactoryGuid { get; }
    }

    public interface IFactory<T> : IFactory where T : IPropertyTemplateFactory
    {
        /// <summary>
        /// Init method for the factory
        /// </summary>
        /// <param name="factory"></param>
        void InitTemplates(T factory);

    }
}
