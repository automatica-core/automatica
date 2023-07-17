using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Synology.DriverFactory.Attributes;
using P3.Synology.Api.Client;

namespace P3.Driver.Synology.DriverFactory
{
    internal class SynologyDevice : DriverBase
    {
        private SynologyConnectedAttribute _connectedAttribute;

        private string _ip;
        private string _username;
        private string _password;
        private bool _useHttps;

        private string _uri;

        private bool _useRemoteConnect;
        private string _remoteConnectDomain;

        private SynologyClient _client;

        private bool _connected;
        private int _port;
        private bool _ignoreSslErrors;

        public SynologyDevice(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<bool> Init(CancellationToken token = new CancellationToken())
        {
            _ip = GetPropertyValueString("ip");
            _port = GetPropertyValueInt("port");
            _username = GetPropertyValueString("user");
            _useHttps = GetProperty("use_https")!.ValueBool!.Value;
            _ignoreSslErrors = GetProperty("ignore_ssl_errors")!.ValueBool!.Value;
            _password = GetPropertyValueString("password");

            _useRemoteConnect = GetProperty("use_remote_connect")!.ValueBool!.Value;
            _remoteConnectDomain = GetPropertyValueString("remote_connect_domain");

            if (_useRemoteConnect)
            {
                var protocol = _useHttps ? TunnelingProtocol.Https : TunnelingProtocol.Http;
                
                await DriverContext.TunnelingProvider.CreateWebTunnelAsync(protocol, Name,
                    _remoteConnectDomain, _ip, _port, null, null, token);
            }

            _uri = (_useHttps ? "https" : "http") + "://" + _ip + ":" + _port;
            

            return await base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            if (!String.IsNullOrEmpty(_ip))
            {
                try
                {
                    var httpClientHandler = new HttpClientHandler();

                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => _ignoreSslErrors;
                var httpClient = new HttpClient(httpClientHandler);
                _client = new SynologyClient(_uri, httpClient);

                
                    if (!String.IsNullOrEmpty(_username) && _password.Length > 0)
                        await _client.LoginAsync(_username, _password);
                    _connected = true;
                    _connectedAttribute.DispatchValue(true);

                }
                catch (Exception e)
                {
                    _connectedAttribute.DispatchValue(false);
                    DriverContext.Logger.LogError(e, $"Could not connected to DSM....{e}");
                }

            }

            return await base.Start(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            _connectedAttribute = new SynologyConnectedAttribute(ctx);
            return _connectedAttribute;
        }
    }
}
