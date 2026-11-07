using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Automatica.Core.Common.Update.Signing
{
    /// <summary>
    /// Public keys that may sign plugin and update packages, pinned in the binary.
    /// To rotate: add the new key here, ship a core with both keys, switch CI to the new private key, then remove the old one.
    /// The private keys are never committed (CI: variable AUTOMATICA_PACKAGE_SIGNING_KEY).
    /// </summary>
    public static class TrustedPackageKeys
    {
        /// <summary>
        /// SubjectPublicKeyInfo (PEM body) of the trusted ECDSA P-256 keys
        /// </summary>
        public static readonly IReadOnlyList<string> PublicKeys = new[]
        {
            // automatica package signing key 1 (2026-11-06), key id see PackageSignatureFormat.GetKeyId
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAERO2fOPb9PkX78iIll7BulsYfx03Qsl7o18p8xupvnfqIXnFe1gOzSLKlcNa1zo/I7K/vB/MHQyqVzWV0HbLz8A=="
        };

        private static readonly Lazy<IReadOnlyList<ECDsa>> PinnedKeys = new(() => Load(PublicKeys));

        /// <summary>
        /// Only for unit tests: replaces the pinned keys, null restores them
        /// </summary>
        internal static IReadOnlyList<ECDsa> OverrideKeys { get; set; }

        public static IReadOnlyList<ECDsa> Keys => OverrideKeys ?? PinnedKeys.Value;

        public static IReadOnlyList<ECDsa> Load(IEnumerable<string> spkiBase64)
        {
            var ret = new List<ECDsa>();
            foreach (var key in spkiBase64)
            {
                var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(key), out _);

                if (ecdsa.KeySize != 256)
                {
                    throw new CryptographicException("Only P-256 package signing keys are supported");
                }
                ret.Add(ecdsa);
            }

            return ret;
        }
    }
}
