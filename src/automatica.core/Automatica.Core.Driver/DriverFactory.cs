using System;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Base implementation of the <see cref="IDriverFactory"/>
    /// </summary>
    public abstract class DriverFactory : IDriverFactory
    {
        /// <summary>
        /// The driverName is mainly used for logging
        /// </summary>
        public abstract string DriverName { get; }
        public abstract  Guid DriverGuid { get; }
        public abstract Version DriverVersion { get; }


        /// <summary>
        /// Indicates that the factory is in development mode and the <see cref="InitNodeTemplates(INodeTemplateFactory)"/> method will be called on every start
        /// </summary>
        public virtual bool InDevelopmentMode => false;

        /// <summary>
        /// The entry point to set your driver definition
        /// </summary>
        /// <param name="factory">Interface to the database to generate your templates</param>
        public abstract void InitNodeTemplates(INodeTemplateFactory factory);

        public abstract IDriver CreateDriver(IDriverContext config);


        /// <summary>
        /// Base empty implementation
        /// </summary>
        /// <param name="instance"><see cref="NodeInstance"/> instance</param>
        public virtual void AfterSave(NodeInstance instance)
        {
            //empty base impl          
        }

        /// <summary>
        /// Base empty implementation
        /// </summary>
        /// <param name="instance"><see cref="NodeInstance"/> instance</param>
        public virtual void AfterDelete(NodeInstance instance)
        {
            //empty base impl
        }

        /// <summary>
        /// Base empty implementation
        /// </summary>
        /// <param name="instance"><see cref="NodeInstance"/> instance</param>
        public virtual void Scan(NodeInstance instance)
        {
            //empty base impl
        }
    }
}
