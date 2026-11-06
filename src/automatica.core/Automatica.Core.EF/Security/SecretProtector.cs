using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Automatica.Core.EF.Security
{
    /// <summary>
    /// Encrypts secrets stored in the database (password properties, api keys) with AES-256-GCM and a random per-install
    /// key in <c>persistent/secrets.key</c> (0600, next to the jwt signing key; the persistent directory is the Docker volume).
    /// Protected values look like <c>enc:v1:&lt;base64(nonce|tag|cipher)&gt;</c>, everything else is treated as plain text,
    /// so existing databases keep working and are encrypted on the next save.
    /// <para>A database backup restored on another installation needs the key file as well.</para>
    /// </summary>
    public static class SecretProtector
    {
        public const string Prefix = "enc:v1:";
        public const string FileName = "secrets.key";

        /// <summary>
        /// Overrides the location of the key file
        /// </summary>
        public const string KeyFileEnv = "AUTOMATICA_SECRETS_KEY_FILE";

        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private static readonly object Lock = new();
        private static byte[] _key;
        private static bool _loaded;

        /// <summary>
        /// For tests: a fixed key (null = the key file)
        /// </summary>
        internal static byte[] OverrideKey { get; set; }

        public static bool IsProtected(string value)
        {
            return value != null && value.StartsWith(Prefix, StringComparison.Ordinal);
        }

        /// <returns>the encrypted value, or the value itself if it is empty, already encrypted or no key could be stored</returns>
        public static string Protect(string value)
        {
            if (string.IsNullOrEmpty(value) || IsProtected(value))
            {
                return value;
            }

            var key = GetKey();
            if (key == null)
            {
                // without a persisted key the value could not be decrypted after a restart
                return value;
            }

            var plain = Encoding.UTF8.GetBytes(value);
            var buffer = new byte[NonceSize + TagSize + plain.Length];
            var nonce = buffer.AsSpan(0, NonceSize);
            RandomNumberGenerator.Fill(nonce);

            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Encrypt(nonce, plain, buffer.AsSpan(NonceSize + TagSize), buffer.AsSpan(NonceSize, TagSize));
            }

            return Prefix + Convert.ToBase64String(buffer);
        }

        /// <returns>false if the value is encrypted but cannot be decrypted (other key, corrupted)</returns>
        public static bool TryUnprotect(string value, out string plain)
        {
            plain = value;
            if (!IsProtected(value))
            {
                return true;
            }

            var key = GetKey();
            if (key == null)
            {
                return false;
            }

            try
            {
                var buffer = Convert.FromBase64String(value.Substring(Prefix.Length));
                if (buffer.Length < NonceSize + TagSize)
                {
                    return false;
                }

                var result = new byte[buffer.Length - NonceSize - TagSize];
                using (var aes = new AesGcm(key, TagSize))
                {
                    aes.Decrypt(buffer.AsSpan(0, NonceSize), buffer.AsSpan(NonceSize + TagSize), buffer.AsSpan(NonceSize, TagSize), result);
                }

                plain = Encoding.UTF8.GetString(result);
                return true;
            }
            catch (Exception e) when (e is FormatException or CryptographicException)
            {
                return false;
            }
        }

        public static string GetKeyFilePath()
        {
            var configured = Environment.GetEnvironmentVariable(KeyFileEnv);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            // same rule as ServerInfo.GetPersistentDirectory (not referenced from here)
            var baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
            return Path.Combine(baseDirectory, "persistent", FileName);
        }

        private static byte[] GetKey()
        {
            if (OverrideKey != null)
            {
                return OverrideKey;
            }

            lock (Lock)
            {
                if (_loaded)
                {
                    return _key;
                }

                _loaded = true;
                var path = GetKeyFilePath();
                try
                {
                    if (File.Exists(path))
                    {
                        var existing = File.ReadAllBytes(path);
                        if (existing.Length >= KeySize)
                        {
                            _key = existing[..KeySize];
                            return _key;
                        }

                        // never replace a key file that exists: values encrypted with it would be lost
                        Console.Error.WriteLine($"The secrets key file {path} is invalid, secrets are stored unencrypted");
                        return null;
                    }

                    var key = RandomNumberGenerator.GetBytes(KeySize);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    Write(path, key);
                    _key = key;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"Could not read or create the secrets key {path}: {e.Message}. Secrets are stored unencrypted.");
                    _key = null;
                }

                return _key;
            }
        }

        private static void Write(string path, byte[] key)
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
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
    }
}
