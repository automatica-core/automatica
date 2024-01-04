using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;


namespace Automatica.Core.UnitTests.Base.Logics
{
    public class LogicInstanceVisuNotifyMock : IRuleInstanceVisuNotify
    {
        public Task NotifyValueChanged(RuleInstance instance, object value)
        {
            return Task.CompletedTask;
        }

        public Task NotifyValueChanged(RuleInterfaceInstance instance, object value)
        {
            return Task.CompletedTask;
        }

        public Task NotifyValueChanged(IDispatchable instance, DispatchValue value)
        {
            return Task.CompletedTask;
        }
    }

    public class LogicContextMock : ILogicContext
    {
        public LogicContextMock(RuleInstance instance, ILogicTemplateFactory factory, IDispatcher dispatcher)
        {
            RuleInstance = instance;
            Notify = new LogicInstanceVisuNotifyMock();
            Dispatcher = dispatcher;
            Factory = factory;
            ControlContext = new Mock<IControlContext>().Object;
        }
        public RuleInstance RuleInstance { get; set; }
        public IDispatcher Dispatcher { get; }
        public ILogicTemplateFactory Factory { get; }

        public IRuleInstanceVisuNotify Notify { get; }

        public ILogger Logger => NullLogger.Instance;
        public IServerCloudApi CloudApi => new CloudApiMock();
        public ILicenseContract LicenseContract => new LicenseContractMock();

        public IControlContext ControlContext { get; }

        public TimeProvider TimeProvider => FakeTimeProvider.Instance;
    }

    public class FakeTimeProvider : TimeProvider
    {
        private DateTime _dateTime = DateTime.Now;

        public static FakeTimeProvider Instance { get; } = new FakeTimeProvider();

        public override DateTimeOffset GetUtcNow()
        {
            return _dateTime.ToUniversalTime();
        }

        public void SetDateTime(DateTime dateTime)
        {
            _dateTime = dateTime;
        }
    }
}
