using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Implementation for <see cref="IDriverContext"/>
    /// </summary>
    public class DriverContext : IDriverContext
    {
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

        public DriverContext(NodeInstance nodeInstance, IDriverFactory factory, IDispatcher dispatcher,
            INodeTemplateFactory nodeTemplateFactory, ITelegramMonitor telegramMonitor, ILicenseState licenseState,
            ILogger logger, ILearnMode learnMode, IServerCloudApi api, ILicenseContract licenseContract, ILoggerFactory loggerFactory, bool isTest)
        {
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
        }
    }
}
