using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.UnitTests.Base.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.UnitTests.Base.Rules
{
    public class RuleInstanceVisuNotifyMock : IRuleInstanceVisuNotify
    {
        public Task NotifyValueChanged(RuleInstance instance, object value)
        {
            return Task.CompletedTask;
        }

        public Task NotifyValueChanged(RuleInterfaceInstance instance, object value)
        {
            return Task.CompletedTask;
        }
    }

    public class RuleContextMock : IRuleContext
    {
        public RuleContextMock(RuleInstance instance, IRuleTemplateFactory factory, IDispatcher dispatcher)
        {
            RuleInstance = instance;
            Notify = new RuleInstanceVisuNotifyMock();
            Dispatcher = dispatcher;
            Factory = factory;
        }
        public RuleInstance RuleInstance { get; }
        public IDispatcher Dispatcher { get; }
        public IRuleTemplateFactory Factory { get; }

        public IRuleInstanceVisuNotify Notify { get; }

        public ILogger Logger => NullLogger.Instance;
        public IServerCloudApi CloudApi => new CloudApiMock();
        public ILicenseContract LicenseContract => new LicenseContractMock();

    }

   
}
