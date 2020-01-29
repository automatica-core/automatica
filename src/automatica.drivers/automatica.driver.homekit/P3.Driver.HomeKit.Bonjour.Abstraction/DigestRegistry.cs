using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Registry of implemented <see cref="DigestType"/>.
    /// </summary>
    /// <remarks>
    ///   IANA maintains a list of all known types at <see href="https://www.iana.org/assignments/ds-rr-types/ds-rr-types.xhtml#ds-rr-types-1"/>.
    /// </remarks>
    /// <see cref="DigestType"/>
    /// <see cref="HashAlgorithm"/>
    public static class DigestRegistry
    {
        /// <summary>
        ///   Defined hashing algorithms.
        /// </summary>
        /// <remarks>
        ///   The key is the <see cref="DigestType"/>.
        ///   The value is a function that returns a new <see cref="ResourceRecord"/>.
        /// </remarks>
        public static Dictionary<DigestType, Func<HashAlgorithm>> Digests;

        static DigestRegistry()
        {
            Digests = new Dictionary<DigestType, Func<HashAlgorithm>>();
            Digests.Add(DigestType.Sha1, () => SHA1.Create());
            Digests.Add(DigestType.Sha256, () => SHA256.Create());
            Digests.Add(DigestType.Sha384, () => SHA384.Create());
            Digests.Add(DigestType.Sha512, () => SHA512.Create());
        }

        /// <summary>
        ///   Gets the hash algorithm for the <see cref="DigestType"/>.
        /// </summary>
        /// <param name="digestType">
        ///   One of the <see cref="DigestType"/> values.
        /// </param>
        /// <returns>
        ///   A new instance of the <see cref="HashAlgorithm"/> that implements
        ///   the <paramref name="digestType"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        ///   When <paramref name="digestType"/> is not implemented.
        /// </exception>
        public static HashAlgorithm Create(DigestType digestType)
        {
            if (Digests.TryGetValue(digestType, out Func<HashAlgorithm> maker)) 
            {
                return maker();
            }
            throw new NotImplementedException($"Digest type '{digestType}' is not implemented.");
        }

        /// <summary>
        ///   Gets the hash algorithm for the <see cref="SecurityAlgorithm"/>.
        /// </summary>
        /// <param name="algorithm">
        ///   One of the <see cref="SecurityAlgorithm"/> values.
        /// </param>
        /// <returns>
        ///   A new instance of the <see cref="HashAlgorithm"/> that is used
        ///   for the <paramref name="algorithm"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        ///   When the <paramref name="algorithm"/> or its <see cref="HashAlgorithm"/>
        ///   is not implemented.
        /// </exception>
        public static HashAlgorithm Create(SecurityAlgorithm algorithm)
        {
            var metadata = SecurityAlgorithmRegistry.GetMetadata(algorithm);
            return Create(metadata.HashAlgorithm);
        }
    }
}
