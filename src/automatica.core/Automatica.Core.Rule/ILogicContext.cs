using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Automatica.Core.Control;
using System;
using Automatica.Core.Base.Localization;
using System.Threading.Tasks;
using System.Threading;

[assembly: InternalsVisibleTo("Automatica.Core.UnitTests.Base")]
namespace Automatica.Core.Logic
{
    /// <summary>
    /// Rule context data
    /// </summary>
    public interface ILogicContext
    {
        /// <summary>
        /// The <see cref="RuleInstance"/> itself
        /// </summary>
        RuleInstance RuleInstance { get; internal set; }

        /// <summary>
        /// The <see cref="IDispatcher"/> instance
        /// </summary>
        IDispatcher Dispatcher { get; }

        /// <summary>
        /// Rule Template Factory
        /// </summary>
        ILogicTemplateFactory Factory { get; }

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

        /// <summary>
        /// Provides the control context
        /// </summary>
        IControlContext ControlContext { get; }

        /// <summary>
        /// Provides date and times (also fake ones for tests)
        /// </summary>
        TimeProvider TimeProvider { get; }

        /// <summary>
        /// Provides access to localizations
        /// </summary>
        ILocalizationProvider LocalizationProvider { get; }

        Task CreateNotification(string subject, string body, NotificationSeverity severity,
            CancellationToken token = default);
    }
}
