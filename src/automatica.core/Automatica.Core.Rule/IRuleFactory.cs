using System;
using Automatica.Core.Base.Templates;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Interface for the 
    /// </summary>
    public interface IRuleFactory
    {
        /// <summary>
        /// The driverName is mainly used for logging
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// Rule Guid needs to be unique across the system
        /// </summary>
        Guid RuleGuid { get; }

        /// <summary>
        /// Rule version indicates if something changed in the <see cref="EF.Models.RuleInterfaceTemplate"/> definition. Increment to define that the <see cref="EF.Models.RuleInterfaceTemplate"/> will be updated
        /// </summary>
        Version RuleVersion { get; }


        /// <summary>
        /// Indicates that the factory is in development mode and the <see cref="InitNodeTemplates(IRuleTemplateFactory)"/> method will be called on every start
        /// </summary>
        bool InDevelopmentMode { get; }

        /// <summary>
        /// Init method for the factory
        /// </summary>
        /// <param name="factory"></param>
        void InitTemplates(IRuleTemplateFactory factory);

        /// <summary>
        /// Returns a new instance of the <see cref="IRule"/>
        /// </summary>
        /// <param name="context">Context paramters</param>
        /// <returns>A new instance of <see cref="IRule"/></returns>

        IRule CreateRuleInstance(IRuleContext context);
    }
}
