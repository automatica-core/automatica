using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
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
                IsTest);
        }

        public DriverContext(NodeInstance nodeInstance, IDriverFactory factory, IDispatcher dispatcher,
            INodeTemplateFactory nodeTemplateFactory, ITelegramMonitor telegramMonitor, ILicenseState licenseState,
            ILogger logger, ILearnMode learnMode, IServerCloudApi api, ILicenseContract licenseContract, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, bool isTest)
        {
            _serviceProvider = serviceProvider;

            NodeInstance = nodeInstance;
            Factory = factory;
            Dispatcher = dispatcher;
            NodeTemplateFactory = nodeTemplateFactory;
            IsTest = isTest;
            TelegramMonitor = telegramMonitor;
            LicenseState = licenseState;
            Logger = logger;
            CloudApi = api;
            LearnMode = learnMode;
            LicenseContract = licenseContract;
            LoggerFactory = loggerFactory;

            var provider = serviceProvider.GetRequiredService<Func<IDriverContext, ITunnelingProvider>>();
            TunnelingProvider = provider.Invoke(this);
        }
    }
}
