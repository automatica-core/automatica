using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Rule context data
    /// </summary>
    public interface IRuleContext
    {
        /// <summary>
        /// The <see cref="RuleInstance"/> itself
        /// </summary>
        RuleInstance RuleInstance { get; }

        /// <summary>
        /// The <see cref="IDispatcher"/> instance
        /// </summary>
        IDispatcher Dispatcher { get; }

        /// <summary>
        /// Communication to the UI
        /// </summary>
        IRuleInstanceVisuNotify Notify { get; }

        /// <summary>
        /// The Logger instance
        /// </summary>
        ILogger Logger { get; }        
        
        /// <summary>
        /// Interface to communicate with the cloud
        /// </summary>
        IServerCloudApi CloudApi { get; }

        /// <summary>
        /// Gives information about the current used license
        /// </summary>
        ILicenseContract LicenseContract { get; }
    }
}
