using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Retry;
using Automatica.Core.Base.Templates;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Control;
using Automatica.Core.Driver.Discovery;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Implementation for <see cref="IDriverContext"/>
    /// </summary>
    public class DriverContext : IDriverContext
    {
        private readonly IServiceProvider _serviceProvider;
        public NodeInstance NodeInstance { get; }
        public IDriverFactory Factory { get; }
        public IDispatcher Dispatcher { get; }
        public INodeTemplateFactory NodeTemplateFactory { get; }
        public bool IsTest { get; }

        public ITelegramMonitor TelegramMonitor { get; }

        public ILicenseState LicenseState { get; }

        public ILogger Logger { get; }

        public ILearnMode LearnMode { get; }
        public IServerCloudApi CloudApi { get; }

        public ILicenseContract LicenseContract { get; }
        public ILoggerFactory LoggerFactory { get; }
        public ITunnelingProvider TunnelingProvider { get; }
        public IZeroconfDiscovery ZeroconfDiscovery { get; }

        public IControlContext ControlContext { get; }
        public TimeProvider TimeProvider { get; }
        public ILocalizationProvider LocalizationProvider { get; }

        public IRetryContext RetryContext { get; }

        private readonly INotificationManager _notificationManager;

        public Task CreateNotification(string subject, string body, NotificationSeverity severity, CancellationToken token = default)
        {
            return _notificationManager.CreateNotification(NodeInstance, LocalizationProvider.GetTranslation(subject),
                LocalizationProvider.GetTranslation(body), severity, token);
        }

        public IDriverContext Copy(NodeInstance node, ILogger logger)
        {
            return new DriverContext(
                node,
                Factory,
                Dispatcher,
                NodeTemplateFactory,
                TelegramMonitor,
                LicenseState,
                logger,
                LearnMode,
                CloudApi,
                LicenseContract,
                LoggerFactory,
                _serviceProvider,
                ControlContext,
                TimeProvider,
                IsTest);
        }

        public DriverContext(NodeInstance nodeInstance, IDriverFactory factory, IDispatcher dispatcher,
            INodeTemplateFactory nodeTemplateFactory, ITelegramMonitor telegramMonitor, ILicenseState licenseState,
            ILogger logger, ILearnMode learnMode, IServerCloudApi api, ILicenseContract licenseContract, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IControlContext controlContext, TimeProvider timeProvider, bool isTest)
        {
            _serviceProvider = serviceProvider;

            NodeInstance = nodeInstance;
            Factory = factory;
            Dispatcher = dispatcher;
            NodeTemplateFactory = nodeTemplateFactory;
            IsTest = isTest;
            ControlContext = controlContext;
            TelegramMonitor = telegramMonitor;
            LicenseState = licenseState;
            Logger = logger;
            CloudApi = api;
            LearnMode = learnMode;
            LicenseContract = licenseContract;
            LoggerFactory = loggerFactory;
            TimeProvider = timeProvider;

            var provider = serviceProvider.GetRequiredService<Func<IDriverContext, ITunnelingProvider>>();
            TunnelingProvider = provider.Invoke(this);

            ZeroconfDiscovery = serviceProvider.GetRequiredService<IZeroconfDiscovery>();

            RetryContext = serviceProvider.GetRequiredService<IRetryContext>();

            LocalizationProvider = serviceProvider.GetRequiredService<ILocalizationProvider>();
            _notificationManager = serviceProvider.GetRequiredService<INotificationManager>();
        }
    }
}
