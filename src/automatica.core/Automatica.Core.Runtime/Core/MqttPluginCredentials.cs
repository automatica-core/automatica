using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Base.Common;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Per-plugin broker credentials. Every plugin container on a satellite gets its own password, derived from a
    /// random per-install secret and bound to the satellite and the node it runs, so a plugin can only connect as its
    /// own node and never as the satellite or as another plugin. The password is sent to the satellite in the start
    /// action (topic <c>slave/&lt;satellite&gt;/action</c>, which only that satellite may read).
    /// </summary>
    internal sealed class MqttPluginCredentials
    {
        public const string FileName = "mqtt-plugin.key";
        private const int KeySize = 32;

        private readonly byte[] _secret;

        public MqttPluginCredentials(byte[] secret)
        {
            if (secret == null || secret.Length < KeySize)
            {
                throw new ArgumentException("secret too short", nameof(secret));
            }
            _secret = secret;
        }

        public static MqttPluginCredentials CreateDefault()
        {
            return new MqttPluginCredentials(LoadOrCreateSecret(Path.Combine(ServerInfo.GetPersistentDirectory(), FileName)));
        }

        public string GetPassword(string satelliteClientId, Guid nodeId)
        {
            using var hmac = new HMACSHA256(_secret);
            var mac = hmac.ComputeHash(Encoding.UTF8.GetBytes($"automatica-mqtt-plugin\n{satelliteClientId}\n{nodeId:D}"));
            return "p1." + Convert.ToBase64String(mac).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public bool Verify(string satelliteClientId, Guid nodeId, string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                return false;
            }
            return SatelliteClientKey.FixedTimeEquals(GetPassword(satelliteClientId, nodeId), password);
        }

        internal static byte[] LoadOrCreateSecret(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var existing = File.ReadAllBytes(path);
                    if (existing.Length >= KeySize)
                    {
                        return existing;
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not read the mqtt plugin key {path}: {e.Message}");
            }

            var key = RandomNumberGenerator.GetBytes(KeySize);
            try
            {
                var options = new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    Share = FileShare.None
                };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }
                using var stream = new FileStream(path, options);
                stream.Write(key, 0, key.Length);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // not persisted: plugin passwords change with every start, the satellites restart their plugins then anyway
                Console.Error.WriteLine($"Could not persist the mqtt plugin key to {path}: {e.Message}");
            }
            return key;
        }
    }
}
