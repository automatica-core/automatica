#nullable enable
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Tunneling;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    internal static class FrpcHelper
    {
        private const string FrpcTransportTemplateFile = "frpc_template.ini";

        private const string FrpcHttpTemplateFile = "frpc_web_http_template.ini";

        private const string FrpcHttpsTemplateFile = "frpc_web_https_template.ini";

        private static async Task<string[]> ReadTemplateFile(string templateFileName, CancellationToken token = default)
        {
            var currentDir = ServerInfo.GetBasePath();

            if (!Directory.Exists(Path.Combine(currentDir, "frp")))
            {
                throw new ArgumentException("Could not find configuration directory!");
            }

            if (!File.Exists(Path.Combine(currentDir, "frp", templateFileName)))
            {
                throw new ArgumentException(
                    $"Could not find config file '{templateFileName}' in {Directory.GetCurrentDirectory()}");
            }

            var templateFile = await File.ReadAllLinesAsync(Path.Combine(currentDir, "frp", templateFileName), token);
            return templateFile;
        }

        public static async Task CreateTransportServiceFileFromTemplate(TunnelingProtocol tunnelingProtocol, string name,
            string address, int targetPort,
            int remotePort, CancellationToken token)
        {
            if (tunnelingProtocol != TunnelingProtocol.Tcp && tunnelingProtocol != TunnelingProtocol.Udp)
                throw new ArgumentException("Only Udp and Tcp are supported!");

            var currentDir = ServerInfo.GetBasePath();

            var templateFile = await ReadTemplateFile(FrpcTransportTemplateFile, token);
            await using var newFile =
                new StreamWriter(Path.Combine(currentDir, "frp", "enabled",
                    $"{name}_{tunnelingProtocol.ToString().ToLowerInvariant()}.ini"));

            foreach (var line in templateFile)
            {
                var newLine = line.Replace("{{name}}", name)
                    .Replace("{{type}}", $"{tunnelingProtocol.ToString().ToLowerInvariant()}")
                    .Replace("{{local_ip}}", address)
                    .Replace("{{local_port}}", $"{targetPort}")
                    .Replace("{{remote_port}}", $"{remotePort}");
                await newFile.WriteLineAsync(newLine);
            }

            newFile.Close();

        }

        public static async Task CreateApplicationServiceFileFromTemplate(TunnelingProtocol tunnelingProtocol, string name,
            string address, int targetPort,
            string subDomain, string basicUser, string basicPassword, CancellationToken token)
        {

            if (tunnelingProtocol != TunnelingProtocol.Http && tunnelingProtocol != TunnelingProtocol.Https)
                throw new ArgumentException("Only Http and Https are supported!");

            var currentDir = ServerInfo.GetBasePath();

            var templateFileName = tunnelingProtocol == TunnelingProtocol.Http
                ? FrpcHttpTemplateFile
                : FrpcHttpsTemplateFile;

            var templateFile = await ReadTemplateFile(templateFileName, token);
            await using var newFile =
                new StreamWriter(Path.Combine(currentDir, "frp", "enabled",
                    $"{name}_{tunnelingProtocol.ToString().ToLowerInvariant()}.ini"));

            foreach (var line in templateFile)
            {
                var newLine = line.Replace("{{name}}", name)
                    .Replace("{{local_ip}}", address)
                    .Replace("{{local_port}}", $"{targetPort}")
                    .Replace("{{sub_domain}}", $"{subDomain}")
                    .Replace("{{http_user}}", $"{basicUser}")
                    .Replace("{{http_pwd}}", $"{basicPassword}")
                    .Replace("{{use_basic}}", String.IsNullOrEmpty(basicPassword) || String.IsNullOrEmpty(basicPassword) ? "#" : "");
                await newFile.WriteLineAsync(newLine);
            }

            newFile.Close();

        }

    }
}
