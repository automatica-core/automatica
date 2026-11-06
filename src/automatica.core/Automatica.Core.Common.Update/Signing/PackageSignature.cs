using System.Collections.Generic;
using Newtonsoft.Json;

namespace Automatica.Core.Common.Update.Signing
{
    /// <summary>
    /// Content of the <c>automatica-signature.json</c> file in the root of a plugin (*.acpkg) or update (*.zip) package.
    /// The signature covers <see cref="PackageSignatureFormat.BuildPayload"/>: type, name, version, rid and the sorted list of
    /// SHA-256 hashes of every other zip entry.
    /// </summary>
    public class PackageSignature
    {
        [JsonProperty("formatVersion")]
        public int FormatVersion { get; set; } = PackageSignatureFormat.FormatVersion;

        [JsonProperty("algorithm")]
        public string Algorithm { get; set; } = PackageSignatureFormat.Algorithm;

        /// <summary>
        /// Hex SHA-256 of the signer's SubjectPublicKeyInfo, first 16 characters (informational, all pinned keys are tried)
        /// </summary>
        [JsonProperty("keyId")]
        public string KeyId { get; set; }

        /// <summary>
        /// <see cref="PackageSignatureFormat.TypePlugin"/> or <see cref="PackageSignatureFormat.TypeUpdate"/>
        /// </summary>
        [JsonProperty("packageType")]
        public string PackageType { get; set; }

        /// <summary>
        /// Plugin: the component name, update: <see cref="PackageSignatureFormat.UpdateName"/>
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("rid")]
        public string Rid { get; set; }

        [JsonProperty("files")]
        public List<PackageSignatureFile> Files { get; set; } = new();

        /// <summary>
        /// Base64 ECDSA P-256 signature (IEEE P1363, r||s) over SHA-256 of the UTF-8 payload
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

    public class PackageSignatureFile
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }
    }
}
