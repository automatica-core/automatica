using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    internal class FrpService : IFrpService
    {
        private readonly IOptionsMonitor<FrpcOptions> _options;
        private readonly IFrpProcess _process;
        private readonly ILogger<FrpService> _logger;

        private bool _isInitialized;

        private readonly string _frpcIniFile = "frpc.ini";
        private readonly string _frpcWebIni = "frpc_automatica-web.ini";
        private readonly string _frpcSshIni = "frpc_ssh.ini";

        public FrpService(IOptionsMonitor<FrpcOptions> options, IFrpProcess process, ILogger<FrpService> logger)
        {
            _options = options;
            _process = process;
            _logger = logger;
        }

        public Task InitConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            var currentDir = ServerInfo.GetBasePath();
            try
            {
                if (!Directory.Exists("frp"))
                {
                    throw new ArgumentException($"Could not find configuration directory in {currentDir}");
                }

                if (!File.Exists(Path.Combine("frp", _frpcIniFile)))
                {
                    throw new ArgumentException(
                        $"Could not find config file '{_frpcIniFile}' in {Directory.GetCurrentDirectory()}");
                }

                if (!Directory.Exists(Path.Combine("frp", "enabled")))
                {
                    Directory.CreateDirectory(Path.Combine("frp", "enabled"));
                }
                else
                {
                    var files = Directory.GetFiles(Path.Combine("frp", "enabled"));

                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }

                if (_options.CurrentValue.UseSsh)
                {
                    InitConfigurationFile(_frpcSshIni);
                }

                if (_options.CurrentValue.UseWeb)
                {
                    InitConfigurationFile(_frpcWebIni);
                }

                _isInitialized = true;
            }
            catch
            {
                throw;
            }
            return Task.CompletedTask;
        }

        private void InitConfigurationFile(string file)
        {
            if (!File.Exists(file))
            {
                _logger.LogError($"Cannot enable ssh, '{file}' could not be found!");
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    File.Copy(file, Path.Combine(Directory.GetCurrentDirectory(), "enabled", file));
                }
                else
                {
                    File.CreateSymbolicLink(Path.Combine(Directory.GetCurrentDirectory(), "enabled", file), file);
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                await InitConfigurationsAsync(cancellationToken);
            }
            await _process.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _process.StopAsync(cancellationToken);
        }
    }
}
