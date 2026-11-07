using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Automatica.Core.Common.Update.Signing
{
    /// <summary>
    /// Signs a package zip in place (adds or replaces automatica-signature.json). The production signer is automatica-cli,
    /// this one is used by tests and tooling.
    /// </summary>
    public static class PackageSigner
    {
        public static PackageSignature Sign(string zipFile, ECDsa key, string packageType, string name, string version, string rid)
        {
            using var archive = ZipFile.Open(zipFile, ZipArchiveMode.Update);

            foreach (var existing in archive.Entries.Where(a => PackageSignatureFormat.IsSignatureEntry(PackageSignatureFormat.NormalizePath(a.FullName))).ToList())
            {
                existing.Delete();
            }

            var files = new List<PackageSignatureFile>();
            foreach (var entry in archive.Entries)
            {
                if (PackageSignatureFormat.IsDirectoryEntry(entry))
                {
                    continue;
                }

                using var stream = entry.Open();
                files.Add(new PackageSignatureFile
                {
                    Path = PackageSignatureFormat.NormalizePath(entry.FullName),
                    Sha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()
                });
            }

            var signature = new PackageSignature
            {
                KeyId = PackageSignatureFormat.GetKeyId(key),
                PackageType = packageType,
                Name = name,
                Version = version,
                Rid = rid ?? string.Empty,
                Files = files.OrderBy(a => a.Path, StringComparer.Ordinal).ToList()
            };

            var payload = PackageSignatureFormat.BuildPayload(signature);
            signature.Signature = Convert.ToBase64String(key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));

            var signatureEntry = archive.CreateEntry(PackageSignatureFormat.SignatureFileName, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(signatureEntry.Open()))
            {
                writer.Write(JsonConvert.SerializeObject(signature, Formatting.Indented));
            }

            return signature;
        }
    }
}
