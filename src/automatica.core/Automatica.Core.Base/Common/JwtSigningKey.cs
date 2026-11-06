using System;
using System.IO;
using System.Security.Cryptography;

namespace Automatica.Core.Base.Common
{
    /// <summary>
    /// Provides the symmetric key used to sign and validate the JWT tokens. The key is random (256 bit),
    /// created on first use and stored in the persistent directory, so it survives restarts but is never
    /// derived from anything that is published (like the server uid).
    /// </summary>
    public static class JwtSigningKey
    {
        public const string FileName = "jwt-signing.key";
        private const int KeySize = 32;

        private static readonly object Lock = new object();
        private static byte[] _key;

        public static byte[] Get()
        {
            lock (Lock)
            {
                if (_key != null)
                {
                    return _key;
                }

                // persistent directory: a Docker volume, so the key (and every login) survives image updates
                var path = Path.Combine(ServerInfo.GetPersistentDirectory(), FileName);

                if (File.Exists(path))
                {
                    var existing = File.ReadAllBytes(path);
                    if (existing.Length >= KeySize)
                    {
                        _key = existing;
                        return _key;
                    }
                }

                var key = RandomNumberGenerator.GetBytes(KeySize);
                try
                {
                    Write(path, key);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // persistent directory not writable: keep the key in memory, tokens are only valid until the next restart
                    Console.Error.WriteLine($"Could not persist jwt signing key to {path}: {e.Message}");
                }

                _key = key;
                return _key;
            }
        }

        private static void Write(string path, byte[] key)
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

            using (var stream = new FileStream(path, options))
            {
                stream.Write(key, 0, key.Length);
            }
        }
    }
}
