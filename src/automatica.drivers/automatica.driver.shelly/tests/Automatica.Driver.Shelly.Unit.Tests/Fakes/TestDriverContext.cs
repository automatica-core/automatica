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
using Automatica.Core.Driver;
using Automatica.Core.Driver.Discovery;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Driver.Shelly.Unit.Tests.Fakes
{
    /// <summary>
    /// <see cref="DriverContextMock"/> with a logger and a dispatcher of the test (the mock always logs to NullLogger).
    /// </summary>
    internal sealed class TestDriverContext : IDriverContext
    {
        private readonly DriverContextMock _inner;

        public TestDriverContext(NodeInstance node, IDriverFactory factory, INodeTemplateFactory templateFactory, IDispatcher dispatcher, ILogger logger)
        {
            _inner = new DriverContextMock(node, factory, templateFactory, dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            Logger = logger;
        }

        public NodeInstance NodeInstance => _inner.NodeInstance;
        public IDriverFactory Factory => _inner.Factory;
        public IDispatcher Dispatcher => _inner.Dispatcher;
        public INodeTemplateFactory NodeTemplateFactory => _inner.NodeTemplateFactory;
        public ITelegramMonitor TelegramMonitor => _inner.TelegramMonitor;
        public ILicenseState LicenseState => _inner.LicenseState;
        public bool IsTest => true;
        public ILogger Logger { get; }
        public ILearnMode LearnMode => _inner.LearnMode;
        public IServerCloudApi CloudApi => _inner.CloudApi;
        public ILicenseContract LicenseContract => _inner.LicenseContract;
        public ILoggerFactory LoggerFactory => _inner.LoggerFactory;
        public ITunnelingProvider TunnelingProvider => _inner.TunnelingProvider;
        public IZeroconfDiscovery ZeroconfDiscovery => _inner.ZeroconfDiscovery;
        public IControlContext ControlContext => _inner.ControlContext;
        public TimeProvider TimeProvider => _inner.TimeProvider;
        public ILocalizationProvider LocalizationProvider => _inner.LocalizationProvider;
        public IRetryContext RetryContext => _inner.RetryContext;

        public Task CreateNotification(string subject, string body, NotificationSeverity severity, CancellationToken token = default) => Task.CompletedTask;

        public IDriverContext Copy(NodeInstance node, ILogger logger) =>
            new TestDriverContext(node, Factory, NodeTemplateFactory, Dispatcher, logger ?? Logger);
    }
}
