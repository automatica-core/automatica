using System;
using Automatica.Core.Base.Templates;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Base implementation of <see cref="IRuleFactory"/>
    /// </summary>
    public abstract class RuleFactory : IRuleFactory
    {
        /// <summary>
        /// The name of the rule
        /// </summary>
        public abstract string RuleName { get; }

        /// <summary>
        /// Rule Guid needs to be unique across the system
        /// </summary>
        public abstract Guid RuleGuid { get; }
        public Guid FactoryGuid => RuleGuid;

        /// <summary>
        /// Rule version indicates if something changed in the <see cref="EF.Models.RuleInterfaceTemplate"/> definition. Increment to define that the <see cref="EF.Models.RuleInterfaceTemplate"/> will be updated
        /// </summary>
        public abstract Version RuleVersion { get; }

        /// <summary>
        /// Indicates that the factory is in development mode and the <see cref="InitNodeTemplates(IRuleTemplateFactory)"/> method will be called on every start
        /// </summary>
        public virtual bool InDevelopmentMode => false;


        /// <summary>
        /// Init method for the factory
        /// </summary>
        /// <param name="factory"></param>
        public abstract void InitTemplates(IRuleTemplateFactory factory);

        /// <summary>
        /// Returns a new instance of the <see cref="IRule"/>
        /// </summary>
        /// <param name="context">Context paramters</param>
        /// <returns>A new instance of <see cref="IRule"/></returns>
        public abstract IRule CreateRuleInstance(IRuleContext context);
    }
}
