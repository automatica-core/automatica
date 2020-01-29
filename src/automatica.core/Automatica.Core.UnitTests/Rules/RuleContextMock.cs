using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
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
    }

    public class RuleContextMock : IRuleContext
    {
        public RuleContextMock(RuleInstance instance, IDispatcher dispatcher)
        {
            RuleInstance = instance;
            Notify = new RuleInstanceVisuNotifyMock();
            Dispatcher = dispatcher;
        }
        public RuleInstance RuleInstance { get; }
        public IDispatcher Dispatcher { get; }

        public IRuleInstanceVisuNotify Notify { get; }

        public ILogger Logger => NullLogger.Instance;
        public IServerCloudApi CloudApi => new CloudApiMock();
        public ILicenseContract LicenseContract => new LicenseContractMock();

    }

   
}
