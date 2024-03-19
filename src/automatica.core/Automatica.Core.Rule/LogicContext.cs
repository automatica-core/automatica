using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control;
using Automatica.Core.EF.Models;
using Automatica.Core.Notification;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logic
{
    /// <summary>
    /// Implementation of the <see cref="ILogicContext"/>
    /// </summary>
    public class LogicContext : ILogicContext
    {
        private readonly INotificationManager _notificationManager;
        public LogicContext(RuleInstance ruleInstance, IDispatcher dispatcher, ILogicTemplateFactory factory, IRuleInstanceVisuNotify notify, ILogger logger, IServerCloudApi api, ILicenseContract licenseContract, IControlContext controlContext, TimeProvider timeProvider, ILocalizationProvider localizationProvider, INotificationManager notificationManager)
        {
            RuleInstance = ruleInstance;
            Dispatcher = dispatcher;
            Factory = factory;
            Notify = notify;
            Logger = logger;
            CloudApi = api;
            LicenseContract = licenseContract;
            ControlContext = controlContext;
            TimeProvider = timeProvider;
            LocalizationProvider = localizationProvider;
            _notificationManager = notificationManager;
        }

        public RuleInstance RuleInstance { get; set; }
        public IDispatcher Dispatcher { get; }
        public ILogicTemplateFactory Factory { get; }
        public IRuleInstanceVisuNotify Notify { get; }

        public ILogger Logger { get; }
        public IServerCloudApi CloudApi { get; }

        public ILicenseContract LicenseContract { get; }

        public IControlContext ControlContext { get; }

        public TimeProvider TimeProvider { get; }

        public ILocalizationProvider LocalizationProvider { get; }
        public Task CreateNotification(string subject, string body, NotificationSeverity severity, CancellationToken token = default)
        {
            return _notificationManager.CreateNotification(RuleInstance, LocalizationProvider.GetTranslation(subject),
                LocalizationProvider.GetTranslation(body), severity, token);
        }
    }
}
