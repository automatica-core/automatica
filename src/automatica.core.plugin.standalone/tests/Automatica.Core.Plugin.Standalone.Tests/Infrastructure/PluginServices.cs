using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Retry;
using Automatica.Core.Base.Templates;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Discovery;
using Automatica.Core.Driver.Loader;
using Automatica.Core.EF.Models;
using Automatica.Core.Notification;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Automatica.Core.Plugin.Standalone.Tests.Infrastructure
{
    /// <summary>
    /// The services MqttConnection and DriverContext resolve, as mocks; one fake driver factory and driver
    /// </summary>
    internal sealed class PluginServices
    {
        public Guid DriverGuid { get; } = Guid.NewGuid();
        public Guid DriverId { get; } = Guid.NewGuid();

        public Mock<IDriverFactory> Factory { get; } = new Mock<IDriverFactory>();
        public Mock<IDriver> Driver { get; } = new Mock<IDriver>();
        public Mock<IDriverFactoryLoader> Loader { get; } = new Mock<IDriverFactoryLoader>();
        public Mock<IDriverNodesStore> NodeStore { get; } = new Mock<IDriverNodesStore>();
        public Mock<IDriverStore> DriverStore { get; } = new Mock<IDriverStore>();
        public Mock<INodeTemplateFactory> TemplateFactory { get; } = new Mock<INodeTemplateFactory>();

        public List<IDriverContext> CreatedContexts { get; } = new List<IDriverContext>();
        public List<NodeInstance> LoadedNodes { get; } = new List<NodeInstance>();

        public IServiceProvider Provider { get; }

        public PluginServices()
        {
            Factory.SetupGet(f => f.DriverGuid).Returns(DriverGuid);
            Factory.SetupGet(f => f.DriverName).Returns("test-driver");
            Factory.Setup(f => f.CreateDriver(It.IsAny<IDriverContext>()))
                .Callback<IDriverContext>(c => { lock (CreatedContexts) { CreatedContexts.Add(c); } })
                .Returns(Driver.Object);

            Driver.SetupGet(d => d.Id).Returns(DriverId);
            Driver.Setup(d => d.Start(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            Loader.Setup(l => l.LoadDriverFactory(It.IsAny<NodeInstance>(), It.IsAny<IDriverFactory>(), It.IsAny<IDriverContext>(), It.IsAny<CancellationToken>()))
                .Callback<NodeInstance, IDriverFactory, IDriverContext, CancellationToken>((n, _, _, _) => { lock (LoadedNodes) { LoadedNodes.Add(n); } })
                .ReturnsAsync(Driver.Object);

            DriverStore.Setup(s => s.All()).Returns(new List<IDriver> { Driver.Object });

            var services = new ServiceCollection();
            services.AddSingleton(TemplateFactory.Object);
            services.AddSingleton(Loader.Object);
            services.AddSingleton(NodeStore.Object);
            services.AddSingleton(DriverStore.Object);
            services.AddSingleton(new Mock<ITunnelingProvider>().Object);
            services.AddTransient<Func<IDriverContext, ITunnelingProvider>>(p => _ => p.GetRequiredService<ITunnelingProvider>());
            services.AddSingleton(new Mock<IZeroconfDiscovery>().Object);
            services.AddSingleton(new Mock<IRetryContext>().Object);
            services.AddSingleton(new Mock<ILocalizationProvider>().Object);
            services.AddSingleton(new Mock<INotificationManager>().Object);
            Provider = services.BuildServiceProvider();
        }

        public IList<IDriverFactory> Factories => new List<IDriverFactory> { Factory.Object };

        public Mock<IDriverNode> AddNode(Guid id)
        {
            var node = new Mock<IDriverNode>();
            node.SetupGet(n => n.Id).Returns(id);
            node.Setup(n => n.EnableLearnMode(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            node.Setup(n => n.DisableLearnMode(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            node.Setup(n => n.Read(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            NodeStore.Setup(s => s.Get(id)).Returns(node.Object);
            return node;
        }

        public string ConfigJson(Guid? nodeId = null)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new NodeInstance
            {
                ObjId = nodeId ?? Guid.NewGuid(),
                This2NodeTemplate = DriverGuid,
                Name = "test-driver-node"
            });
        }

        internal MqttConnection CreateConnection(ListLogger logger, string host = "127.0.0.1", string nodeId = null)
        {
            return new MqttConnection(logger, host, nodeId ?? Guid.NewGuid().ToString(), TestKeys.SatelliteUser, TestKeys.PluginPassword, Factories, Provider);
        }
    }
}
