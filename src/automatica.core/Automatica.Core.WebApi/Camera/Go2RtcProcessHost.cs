using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Camera
{
    /// <summary>
    /// Runs the installed go2rtc (https://github.com/AlexxIT/go2rtc) for the camera live view, bound to 127.0.0.1 only:
    /// the browser never talks to go2rtc, the camera endpoint relays it with the normal authentication.
    /// Nothing happens when server:go2rtc_url points to an own go2rtc, when no binary is installed, or when
    /// something answers on the port already. The configuration is rewritten on every start, the camera driver
    /// registers its streams at runtime (so camera passwords do not pile up in the file).
    /// </summary>
    public sealed class Go2RtcProcessHost : BackgroundService
    {
        private static readonly TimeSpan MaxRestartDelay = TimeSpan.FromMinutes(1);

        private readonly Go2RtcProxy _proxy;
        private readonly ILogger<Go2RtcProcessHost> _logger;

        public Go2RtcProcessHost(Go2RtcProxy proxy, ILogger<Go2RtcProcessHost> logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public static string ConfigYaml(int port) =>
            "# written by Automatica on every start, changes are lost\n" +
            "api:\n" +
            $"  listen: \"127.0.0.1:{port}\"\n" +
            "rtsp:\n" +
            "  listen: \"\"\n" +
            "webrtc:\n" +
            "  listen: \"\"\n" +
            "srtp:\n" +
            "  listen: \"\"\n" +
            "rtmp:\n" +
            "  listen: \"\"\n" +
            "log:\n" +
            "  level: warn\n";

        /// <summary>
        /// server:go2rtc_path, else go2rtc next to the server, else the usual install locations and the PATH
        /// </summary>
        public static string FindBinary(string configured, Func<string, bool> exists = null)
        {
            exists ??= File.Exists;
            var name = OperatingSystem.IsWindows() ? "go2rtc.exe" : "go2rtc";

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return exists(configured) ? configured : null;
            }

            var candidates = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, name),
                Path.Combine(AppContext.BaseDirectory, "go2rtc", name)
            };
            if (!OperatingSystem.IsWindows())
            {
                candidates.Add("/usr/local/bin/go2rtc");
                candidates.Add("/usr/bin/go2rtc");
            }
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            candidates.AddRange(path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(p => Path.Combine(p.Trim(), name)));

            return candidates.FirstOrDefault(exists);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = _proxy.Settings;
            if (settings.IsExternal)
            {
                _logger.LogInformation($"Camera streams via go2rtc at {settings.BaseUri}");
                return;
            }

            var binary = FindBinary(settings.BinaryPath);
            if (binary == null)
            {
                _logger.LogInformation("go2rtc is not installed, camera live views need it (install it or set server:go2rtc_url)");
                return;
            }

            if (await _proxy.IsAvailableAsync(stoppingToken))
            {
                _logger.LogInformation($"go2rtc answers on {settings.BaseUri} already, not starting another one");
                return;
            }

            string configFile;
            try
            {
                var directory = Path.Combine(ServerInfo.GetPersistentDirectory(), "go2rtc");
                Directory.CreateDirectory(directory);
                configFile = Path.Combine(directory, "go2rtc.yaml");
                await File.WriteAllTextAsync(configFile, ConfigYaml(Go2RtcSettings.ManagedPort), stoppingToken);
                ServerInfo.MakeFilePrivate(configFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Could not write the go2rtc configuration, camera live views are not available");
                return;
            }

            var failures = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                var started = DateTimeOffset.UtcNow;
                try
                {
                    await RunAsync(binary, configFile, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"go2rtc ({binary}) could not be started");
                }

                // a process that ran for a while resets the back-off
                failures = DateTimeOffset.UtcNow - started > TimeSpan.FromMinutes(5) ? 1 : failures + 1;
                var delay = TimeSpan.FromSeconds(Math.Min(MaxRestartDelay.TotalSeconds, Math.Pow(2, failures)));
                _logger.LogWarning($"go2rtc stopped, restarting in {delay.TotalSeconds}s");
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task RunAsync(string binary, string configFile, CancellationToken stoppingToken)
        {
            var info = new ProcessStartInfo(binary)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(configFile)!
            };
            info.ArgumentList.Add("-config");
            info.ArgumentList.Add(configFile);

            using var process = new Process { StartInfo = info, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => Log(e.Data);
            process.ErrorDataReceived += (_, e) => Log(e.Data);

            if (!process.Start())
            {
                throw new InvalidOperationException("Process.Start returned false");
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _logger.LogInformation($"go2rtc started ({binary}, pid {process.Id}) on 127.0.0.1:{Go2RtcSettings.ManagedPort}");

            try
            {
                await process.WaitForExitAsync(stoppingToken);
                _logger.LogWarning($"go2rtc exited with code {process.ExitCode}");
            }
            finally
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(true);
                        await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not stop go2rtc");
                    }
                }
            }
        }

        private void Log(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            // go2rtc logs "<time> WRN ..." / "ERR" / "INF"; the configured level is warn, so most lines are worth a warning
            if (line.Contains(" ERR ") || line.Contains(" FTL "))
            {
                _logger.LogError($"go2rtc: {line}");
            }
            else if (line.Contains(" WRN "))
            {
                _logger.LogWarning($"go2rtc: {line}");
            }
            else
            {
                _logger.LogInformation($"go2rtc: {line}");
            }
        }
    }
}
