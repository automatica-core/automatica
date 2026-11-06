using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Internals.Cache.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    internal class FrpProcess : IFrpProcess
    {
        private readonly IFrpcApiClient _apiClient;
        private readonly IOptionsMonitor<FrpcOptions> _settings;
        private readonly IConfiguration _config;
        private readonly ISettingsCache _settingsCache;
        private readonly FrpcCredentials _credentials;
        private readonly ILogger _logger;

        private readonly string _processName = "frpc";

        private readonly Timer _stdTimer = new Timer();
        private Process? _process;

        public FrpProcess(IFrpcApiClient apiClient, IOptionsMonitor<FrpcOptions> settings, IConfiguration config, ISettingsCache settingsCache,
            FrpcCredentials credentials, ILogger<FrpProcess> logger)
        {
            _credentials = credentials;
            _apiClient = apiClient;
            _settings = settings;
            _config = config;
            _settingsCache = settingsCache;
            _logger = logger;

            _stdTimer.Interval = 1000;
        }



        public async Task StartAsync(CancellationToken token = default)
        {
            await KillExistingProcessesAsync();

            var processInformation = GetProcessStartInfo();

            // the environment holds the frps token, the admin password and the cloud api key: never log the values
            _logger.LogInformation($"Starting frpc process ({processInformation.Arguments}), server {_settings.CurrentValue.ServerAddress}:{_settings.CurrentValue.ServerPort}, " +
                                   $"token {(string.IsNullOrEmpty(_credentials.FrpsToken) ? "not set" : "set")}");

            // EnableRaisingEvents has to be set, otherwise Exited is never raised
            var process = new Process
            {
                StartInfo = processInformation,
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                _logger.LogError("Frp process exited");
            };

            process.OutputDataReceived += (sender, args) =>
            {
                _logger.LogInformation($"stdout: {args.Data}");
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                _logger.LogError($"stderr: {args.Data}");
            };

            if (!process.Start())
            {
                process.Dispose();
                throw new InvalidOperationException("Could not start process");
            }

            var previous = Interlocked.Exchange(ref _process, process);
            previous?.Dispose();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
            while (!await _apiClient.IsReady(cancellationToken))
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await KillExistingProcessesAsync();

            Interlocked.Exchange(ref _process, null)?.Dispose();
        }

        private async Task KillExistingProcessesAsync()
        {
            var existingProcesses = Process
                .GetProcessesByName(_processName)
                .ToArray();
            if (!existingProcesses.Any())
                return;

            try
            {
                _logger.LogDebug("Killing existing frpc processes");

                foreach (var existingProcess in existingProcesses)
                {
                    existingProcess.Kill();
                    await existingProcess.WaitForExitAsync();
                }
            }
            finally
            {
                foreach (var existingProcess in existingProcesses)
                {
                    existingProcess.Dispose();
                }
            }
        }

        private ProcessStartInfo GetProcessStartInfo()
        {
            var localPort = ServerInfo.SslWebPort;

            if (String.IsNullOrEmpty(localPort))
            {
                localPort = "443";
            }

            var processStartInfo = new ProcessStartInfo(
                GetExecutableFileName(), $"-c {"frpc.ini"}")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false, //RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                WorkingDirectory = Path.Combine(ServerInfo.GetBasePath(), "frp"),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                EnvironmentVariables =
                {
                    {"ADMIN_PORT", $"{_settings.CurrentValue.AdminPort}"},
                    {"REMOTE_URL", $"{_settings.CurrentValue.ServerAddress}"},
                    {"REMOTE_PORT", $"{_settings.CurrentValue.ServerPort}"},
                    {"LOG_FILE", $"{_settings.CurrentValue.LogFile}"},
                    {"LOG_LEVEL", $"{_settings.CurrentValue.LogLevel}"},
                    
                    {"LOCAL_IP", $"{_settings.CurrentValue.LocalIp}"},
                    {"LOCAL_PORT", $"{localPort}"},
                    {"FRPC_USERNAME", $"{ServerInfo.ServerUid}"},
                    {"FRPS_TOKEN", _credentials.FrpsToken},
                    // lets the frps login plugin of the cloud check that the server uid belongs to this installation
                    {"FRPC_APIKEY", _settingsCache.GetByKey("apiKey")?.ValueText ?? string.Empty},
                    {"ADMIN_USER", _credentials.AdminUser},
                    {"ADMIN_PWD", _credentials.AdminPassword},
                    {"SUB_DOMAIN", $"{_settingsCache.GetByKey("remoteDomain").ValueText ?? _config["db:remoteDomain"]}"}
                }
            };
            return processStartInfo;
        }
        private string GetExecutableFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (File.Exists(Path.Combine("frp", "frpc.exe")))
                {
                    return Path.Combine("frp", "frpc.exe");
                }
            }
            else
            {
                if (File.Exists(Path.Combine("frp", "frpc")))
                {
                    return Path.Combine("frp", "frpc");
                }
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{_processName}.exe" : $"{_processName}";
        }
    }
}
