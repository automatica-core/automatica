using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.UnitTests.Base.Drivers
{
    public class DriverContextMock : IDriverContext
    {
        public DriverContextMock(NodeInstance nodeInstance, IDriverFactory factory, INodeTemplateFactory nodeTemplateFactory, IDispatcher dispatcher, ILoggerFactory loggerFactory)
        {
            NodeInstance = nodeInstance;
            NodeTemplateFactory = nodeTemplateFactory;
            TelegramMonitor = new TelegramMonitorMock();
            LicenseState = new LicenseStateMock();
            Dispatcher = dispatcher;
            LoggerFactory = loggerFactory;
            Factory = factory;
        }

        public NodeInstance NodeInstance { get; }
        public IDriverFactory Factory { get; }
        public IDispatcher Dispatcher { get; }
        public INodeTemplateFactory NodeTemplateFactory { get; }
        public bool IsTest => true;

        public ITelegramMonitor TelegramMonitor { get; }

        public ILicenseState LicenseState { get;}

        public ILogger Logger => NullLogger.Instance;
        public ILearnMode LearnMode => null;
        public IServerCloudApi CloudApi => new CloudApiMock();
        public ILicenseContract LicenseContract => new LicenseContractMock();
        public ILoggerFactory LoggerFactory { get; }
    }
}
