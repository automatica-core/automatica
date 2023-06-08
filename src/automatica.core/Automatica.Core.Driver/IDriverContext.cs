using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Context data for the <see cref="IDriverNode"/>
    /// </summary>
    public interface IDriverContext
    {
        /// <summary>
        /// The <see cref="NodeInstance"/> model
        /// </summary>
        NodeInstance NodeInstance { get; }

        /// <summary>
        /// Corresponding driver factory
        /// </summary>
        IDriverFactory Factory { get; }

        /// <summary>
        /// <see cref="IDispatcher"/> instance
        /// </summary>
        IDispatcher Dispatcher { get; }

        /// <summary>
        /// <see cref="INodeTemplateFactory"/> instance
        /// </summary>
        INodeTemplateFactory NodeTemplateFactory { get; }

        /// <summary>
        /// <see cref="ITelegramMonitor"/> instance
        /// </summary>
        ITelegramMonitor TelegramMonitor { get; }

        /// <summary>
        /// Gets info about the current license
        /// </summary>
        ILicenseState LicenseState { get; }

        /// <summary>
        /// Indicates if the <see cref="IDriverNode"/> is started via a UnitTest
        /// </summary>
        bool IsTest { get; }

        /// <summary>
        /// The Logger instance
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// LearnMode interface
        /// </summary>
        ILearnMode LearnMode { get; }

        /// <summary>
        /// Interface to communicate with the cloud
        /// </summary>
        IServerCloudApi CloudApi { get; }

        /// <summary>
        /// Gives information about the current used license
        /// </summary>
        ILicenseContract LicenseContract { get; }

        /// <summary>
        /// Logger Factory to create custom logger instances
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Tunneling provider to create http/tcp tunnels
        /// </summary>
        ITunnelingProvider TunnelingProvider { get; }

        IDriverContext Copy(NodeInstance node, ILogger logger);
    }

}
