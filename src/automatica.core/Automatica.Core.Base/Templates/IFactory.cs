using System;

namespace Automatica.Core.Base.Templates
{
    public interface IFactory<T> where T : IPropertyTemplateFactory
    {
        /// <summary>
        /// Init method for the factory
        /// </summary>
        /// <param name="factory"></param>
        void InitTemplates(T factory);

        Guid FactoryGuid { get; }

    }
}
