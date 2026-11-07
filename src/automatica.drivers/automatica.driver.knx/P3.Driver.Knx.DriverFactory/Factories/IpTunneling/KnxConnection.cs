#nullable enable annotations
using Knx.Falcon.Configuration;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using Knx.Falcon;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security;
using Automatica.Core.Driver;
using System;
using Docker.DotNet.Models;
using Automatica.Core.Base.Cryptography;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Monitor;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Factories.IpTunneling
{
    internal class KnxSecureSettings
    {
        public string AuthPassword { get; }
        public string UserPassword { get; }
        public int UserId { get; }
        public string IaAddress { get; }

        public KnxSecureSettings(string authPassword, string userPassword, int userId, string iaAddress)
        {
            AuthPassword = authPassword;
            UserPassword = userPassword;
            UserId = userId;
            IaAddress = iaAddress;
        }
    }

    internal class KnxConnection
    {
        private readonly KnxDriver _driver;
        private readonly ILogger _logger;
        private readonly string _ipAddress;
        private readonly bool _useNat;
        private readonly int _port;
        private readonly bool _secureDriver;
        private readonly KnxSecureSettings _secureSettings;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private IPAddress _remoteIp;
        private int _remotePort;
        private KnxBus _tunneling;

        public KnxConnection(KnxDriver driver, ILogger logger, string ipAddress, bool useNat, int port, bool secureDriver, KnxSecureSettings? secureSettings)
        {
            _driver = driver;
            _logger = logger;
            _ipAddress = ipAddress;
            _useNat = useNat;
            _port = port;
            _secureDriver = secureDriver;
            _secureSettings = secureSettings;

            ConstructTunnelingConnection();
        }

        private void ConstructTunnelingConnection()
        {
            _logger.LogInformation($"Construct KNX driver...");

            var remoteIp = IPAddress.Parse(_ipAddress);
            _remoteIp = remoteIp;
            _remotePort = _port;
            ;
            var ip = new IpTunnelingConnectorParameters(_ipAddress, ipPort: _port, useNat: _useNat)
            {
                AutoReconnect = false
            };

            if (_secureDriver)
            {
                var authPw = _secureSettings.AuthPassword;
                var userPw = _secureSettings.UserPassword;
                var userId = _secureSettings.UserId;
                var iaAddress = _secureSettings.IaAddress;

                ip.IndividualAddress = IndividualAddress.Parse(iaAddress);
                ip.DeviceAuthenticationCodeHash =
                    IpUnicastConnectorParameters.GetDeviceAuthenticationCodeHash(PasswordToSecureString(authPw));
                ip.UserPasswordHash =
                    IpUnicastConnectorParameters.GetUserPasswordHash(PasswordToSecureString(userPw));
                ip.UserId = (byte)userId;
                ip.ProtocolType = IpProtocol.Tcp;
            }

            _tunneling = new KnxBus(ip);
            _tunneling.ConnectionStateChanged += _tunneling_ConnectionStateChanged;
            _tunneling.GroupMessageReceived += _tunneling_GroupMessageReceived;

            _logger.LogInformation($"Construct KNX driver...done");
        }

        public async Task StartConnection(CancellationToken token = default)
        {
            _logger.LogInformation($"Start KNX connection...");
            _driver.GatewayState?.SetGatewayState(false);
            await _semaphore.WaitAsync(token);
            try
            {
                await _tunneling.ConnectAsync(token);
                _logger.LogInformation($"Start KNX connection...done");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error connecting to KNX Interface {e}");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> Read(string address)
        {
            _logger.LogDebug($"Read datagram on GA {address}");

            await _semaphore.WaitAsync();
            try
            {
                if (_tunneling.ConnectionState != BusConnectionState.Connected)
                {
                    _logger.LogError($"Cannot read from KNX interface, not connected");
                    return false;
                }

                return await _tunneling.RequestGroupValueAsync(GroupAddress.Parse(address));
            }
            catch (Exception e)
            {
                _logger.LogError($"{address}: Could not read value {e}");
                return false;
            }
            finally
            {
                _semaphore.Release(1);
            }
        }

        public async Task<bool> Write(KnxGroupAddress source, string address, GroupValue groupValue, CancellationToken token)
        {
            _logger.LogDebug($"Write datagram on GA {address} {groupValue.Value.ToHex(false)}");

            await _semaphore.WaitAsync(token);
            try
            {
                if (_tunneling.ConnectionState != BusConnectionState.Connected)
                {
                    _logger.LogError($"Cannot write to KNX interface, not connected");
                    return false;
                }

             
                return await _tunneling.WriteGroupValueAsync(GroupAddress.Parse(address), groupValue,
                    MessagePriority.High, token);
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error writing to KNX interface {address} {groupValue.Value.ToHex(true)} {e}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async void _tunneling_ConnectionStateChanged(object sender, EventArgs e)
        {
            try
            {
                _logger.LogError($"Connection state changed to {_tunneling.ConnectionState}");

                var state = _tunneling.ConnectionState == BusConnectionState.Connected;
                _driver.GatewayState?.SetGatewayState(state);

                await _driver.ConnectionStateChanged(_tunneling.ConnectionState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling KNX connection state change {ex}");
            }
        }

        private async void _tunneling_GroupMessageReceived(object sender, GroupEventArgs e)
        {
            try
            {
                _logger.LogDebug($"Datagram on GA {e.DestinationAddress} {e.EventType}");

                if (e.Value is { Value: not null })
                {
                    await _driver.InternalTelegramMonitorInstance.NotifyTelegram(TelegramDirection.Input, e.SourceAddress, e.DestinationAddress,
                        e.Value.Value.ToHex(true), Automatica.Core.Driver.Utility.Utils.ByteArrayToString(e.Value.Value.AsSpan()));
                }

                if (e.EventType == GroupEventType.ValueRead)
                {
                    var ga = e.DestinationAddress.ToString()!;
                    var retValue = _driver.GetGroupValue(e.DestinationAddress);

                    if (retValue != null)
                    {
                        _logger.LogDebug($"Answer read request on GA {e.DestinationAddress}");

                        await _tunneling.RespondGroupValueAsync(GroupAddress.Parse(ga), retValue);
                    }
                }
                else
                {
                    _driver.GroupValueReceived(e);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling KNX datagram on GA {e?.DestinationAddress} {ex}");
            }
        }

        public async Task DisposeConnection()
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.LogInformation($"Dispose KNX driver...");
                _tunneling.ConnectionStateChanged -= _tunneling_ConnectionStateChanged;
                _tunneling.GroupMessageReceived -= _tunneling_GroupMessageReceived;

                await _tunneling.DisposeAsync();

                _logger.LogInformation($"Dispose KNX driver...done");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error disposing connection properly....");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static SecureString PasswordToSecureString(string password)
        {
            var secureString = new SecureString();

            foreach (char c in password)
                secureString.AppendChar(c);
            return secureString;
        }
    }
}
