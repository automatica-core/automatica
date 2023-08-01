using System;
using Automatica.Core.Base.Templates;

namespace Automatica.Core.Logic
{
    /// <summary>
    /// Base implementation of <see cref="ILogicFactory"/>
    /// </summary>
    public abstract class LogicFactory : ILogicFactory
    {
        /// <summary>
        /// The name of the rule
        /// </summary>
        public abstract string LogicName { get; }

        /// <summary>
        /// Rule Guid needs to be unique across the system
        /// </summary>
        public abstract Guid LogicGuid { get; }
        public Guid FactoryGuid => LogicGuid;

        /// <summary>
        /// Rule version indicates if something changed in the <see cref="EF.Models.RuleInterfaceTemplate"/> definition. Increment to define that the <see cref="EF.Models.RuleInterfaceTemplate"/> will be updated
        /// </summary>
        public abstract Version LogicVersion { get; }

        /// <summary>
        /// Indicates that the factory is in development mode and the <see cref="InitNodeTemplates(ILogicTemplateFactory)"/> method will be called on every start
        /// </summary>
        public virtual bool InDevelopmentMode => false;


        /// <summary>
        /// Init method for the factory
        /// </summary>
        /// <param name="factory"></param>
        public abstract void InitTemplates(ILogicTemplateFactory factory);

        /// <summary>
        /// Returns a new instance of the <see cref="ILogic"/>
        /// </summary>
        /// <param name="context">Context paramters</param>
        /// <returns>A new instance of <see cref="ILogic"/></returns>
        public abstract ILogic CreateLogicInstance(ILogicContext context);
    }
}
