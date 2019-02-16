using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.UnitTests.Drivers
{
    public class DriverContextMock : IDriverContext
    {
        public DriverContextMock(NodeInstance nodeInstance, INodeTemplateFactory nodeTemplateFactory)
        {
            NodeInstance = nodeInstance;
            NodeTemplateFactory = nodeTemplateFactory;
            TelegramMonitor = new TelegramMonitorMock();
            LicenseState = new LicenseStateMock();
        }

        public NodeInstance NodeInstance { get; }
        public IDispatcher Dispatcher => DispatcherMock.Instance;
        public INodeTemplateFactory NodeTemplateFactory { get; }
        public bool IsTest => true;

        public ITelegramMonitor TelegramMonitor { get; }

        public ILicenseState LicenseState { get;}

        public ILogger Logger => NullLogger.Instance;
        public ILearnMode LearnMode => null;
        public IServerCloudApi CloudApi => new CloudApiMock();
        public ILicenseContract LicenseContract => new LicenseContractMock();
    }
}
