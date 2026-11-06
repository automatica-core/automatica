using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Automatica.Core.Common.Update.Signing
{
    /// <summary>
    /// The canonical package signature format. automatica-cli (automatica.core.cli, PackageSigning.cs) implements the same
    /// payload, both must stay byte identical.
    /// </summary>
    public static class PackageSignatureFormat
    {
        public const int FormatVersion = 1;
        public const string Algorithm = "ECDSA-P256-SHA256";
        public const string SignatureFileName = "automatica-signature.json";
        public const string TypePlugin = "plugin";
        public const string TypeUpdate = "update";
        public const string UpdateName = "Automatica.Core";

        private const string PayloadHeader = "AUTOMATICA-PACKAGE-SIGNATURE-V1";

        /// <summary>
        /// Normalizes a zip entry or relative file path: forward slashes, no leading slash
        /// </summary>
        public static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimStart('/');
        }

        public static bool IsDirectoryEntry(ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\");
        }

        public static bool IsSignatureEntry(string normalizedPath)
        {
            return string.Equals(normalizedPath, SignatureFileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The signed payload. Lines end with \n, files are sorted ordinally by path and written as "&lt;sha256 hex&gt; &lt;path&gt;".
        /// </summary>
        public static byte[] BuildPayload(string packageType, string name, string version, string rid, IEnumerable<PackageSignatureFile> files)
        {
            var sb = new StringBuilder();
            void Line(string value)
            {
                if (value != null && (value.Contains('\n') || value.Contains('\r')))
                {
                    throw new InvalidDataException("Line breaks are not allowed in signed values");
                }
                sb.Append(value).Append('\n');
            }

            var sorted = files.OrderBy(a => a.Path, StringComparer.Ordinal).ToList();

            Line(PayloadHeader);
            Line($"type={packageType}");
            Line($"name={name}");
            Line($"version={version}");
            Line($"rid={rid ?? string.Empty}");
            Line($"files={sorted.Count}");
            foreach (var file in sorted)
            {
                Line($"{file.Sha256.ToLowerInvariant()} {file.Path}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] BuildPayload(PackageSignature signature)
        {
            return BuildPayload(signature.PackageType, signature.Name, signature.Version, signature.Rid, signature.Files);
        }

        /// <returns>hex SHA-256 of the SubjectPublicKeyInfo, first 16 characters</returns>
        public static string GetKeyId(ECDsa key)
        {
            return Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo())).ToLowerInvariant()[..16];
        }
    }
}
