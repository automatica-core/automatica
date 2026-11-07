using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    /// <summary>
    /// Secrets of the frpc process:
    /// <list type="bullet">
    /// <item>the frps auth token: from the cloud (createRemoteConnect response), or <c>AUTOMATICA_FRPS_TOKEN</c> /
    /// <c>server:frps_token</c> for self-hosted frps</item>
    /// <item>user and password of the frpc admin api on 127.0.0.1, random per start (any local process could otherwise
    /// reconfigure the tunnel)</item>
    /// </list>
    /// </summary>
    public class FrpcCredentials
    {
        public const string FrpsTokenEnv = "AUTOMATICA_FRPS_TOKEN";
        public const string FrpsTokenConfigKey = "server:frps_token";

        private readonly IConfiguration _config;
        private string? _cloudToken;

        public FrpcCredentials(IConfiguration config)
        {
            _config = config;
        }

        public string AdminUser { get; } = "automatica";
        public string AdminPassword { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

        public string FrpsToken
        {
            get
            {
                var configured = Environment.GetEnvironmentVariable(FrpsTokenEnv);
                if (string.IsNullOrEmpty(configured))
                {
                    configured = _config?[FrpsTokenConfigKey];
                }

                return !string.IsNullOrEmpty(configured) ? configured : _cloudToken ?? string.Empty;
            }
        }

        public void SetCloudToken(string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _cloudToken = token;
            }
        }
    }
}
