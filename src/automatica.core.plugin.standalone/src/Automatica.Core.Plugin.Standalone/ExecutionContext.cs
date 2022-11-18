using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Plugin.Standalone.Abstraction;
using Automatica.Core.Plugin.Standalone.Factories;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.Plugin.Standalone
{
    internal class ExecutionContext
    {
        private readonly ILogger _logger;
        private readonly string _pluginDir;
        private IList<IDriverFactory> _factories = new List<IDriverFactory>();
        private readonly string _masterAddress = "localhost";
        private readonly string _user;
        private readonly string _password;
        private readonly string _nodeId;
        private readonly IServiceProvider _serviceProvider;

        private readonly List<IDriverConnection> _connections = new List<IDriverConnection>();


        public ExecutionContext(ILogger logger, string pluginDir)
        {
            _logger = logger;
            _pluginDir = pluginDir;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAutomaticaDrivers();
            serviceCollection.AddSingleton<ILogger>(a => logger);
            serviceCollection.AddSingleton<ILicenseContract, RemoteLicenseContract>();
            serviceCollection.AddSingleton<INodeTemplateFactory, RemoteNodeTemplatesFactory>();

            _serviceProvider = serviceCollection.BuildServiceProvider();


            var masterEnv = Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_MASTER");
            if (!string.IsNullOrEmpty(masterEnv))
            {
                _masterAddress = masterEnv;
            }

            _user = Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_USER");
            _password = Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_PASSWORD");
            _nodeId = Environment.GetEnvironmentVariable("AUTOMATICA_NODE_ID");
        }

        public async Task<bool> Start()
        {
            if (!await InitPlugin())
            {
                return false;
            }

            return await StartMqtt();
        }

        public async Task Run()
        {
            var tasks = _connections.Select(a => a.Run()).ToList();
            await Task.WhenAny(tasks);

            await Stop();

        }

        private async Task<bool> StartMqtt()
        {
            Console.WriteLine($"Trying to connect to {_masterAddress} with user {_user} and clientId {_nodeId}");
            try
            {
                var connection = new MqttConnection(_logger, _masterAddress,  _nodeId, _user, _password, _factories, _serviceProvider);
                if (!await connection.Start())
                {
                    return false;
                }

                _connections.Add(connection);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not start mqtt connection");
                return false;
            }

            return true;
        }

        private async Task<bool> InitPlugin()
        {
            try
            {
                var localization = new LocalizationProvider(_logger);
                var factories = await Dockerize.Init<DriverFactory>(_pluginDir, _logger, localization);
                var templateFactory = _serviceProvider.GetRequiredService<INodeTemplateFactory>();

                foreach (var factory in factories)
                {
                    _logger.LogDebug($"Loaded {factory}");

                    _logger.LogDebug($"Init templates for {factory}");
                    Dockerize.InitDriverFactory(factory, templateFactory);
                }
                foreach (var factory in factories)
                {
                    _factories.Add(factory);
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not init plugins..{e}");
            }

            return false;
        }

        public async Task<bool> Stop()
        {
            foreach (var connection in _connections)
            {
                await connection.Stop();
            }

            return true;
        }
    }
}
