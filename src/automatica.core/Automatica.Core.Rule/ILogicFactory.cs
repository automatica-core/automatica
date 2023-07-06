using System;
using Automatica.Core.Base.Templates;

namespace Automatica.Core.Logic
{
    /// <summary>
    /// Interface for the 
    /// </summary>
    public interface ILogicFactory : IFactory<ILogicTemplateFactory>
    {
        /// <summary>
        /// The driverName is mainly used for logging
        /// </summary>
        string LogicName { get; }

        /// <summary>
        /// Rule Guid needs to be unique across the system
        /// </summary>
        Guid LogicGuid { get; }

        /// <summary>
        /// Rule version indicates if something changed in the <see cref="EF.Models.RuleInterfaceTemplate"/> definition. Increment to define that the <see cref="EF.Models.RuleInterfaceTemplate"/> will be updated
        /// </summary>
        Version LogicVersion { get; }


        /// <summary>
        /// Indicates that the factory is in development mode and the <see cref="InitNodeTemplates(ILogicTemplateFactory)"/> method will be called on every start
        /// </summary>
        bool InDevelopmentMode { get; }

        /// <summary>
        /// Returns a new instance of the <see cref="ILogic"/>
        /// </summary>
        /// <param name="context">Context paramters</param>
        /// <returns>A new instance of <see cref="ILogic"/></returns>

        ILogic CreateLogicInstance(ILogicContext context);
    }
}
