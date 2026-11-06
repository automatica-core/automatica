using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Core.Common.Update.Signing
{
    public enum PackageVerificationStatus
    {
        Valid,
        /// <summary>
        /// The package has no signature file
        /// </summary>
        Unsigned,
        Invalid
    }

    public class PackageVerificationResult
    {
        public PackageVerificationStatus Status { get; private init; }
        public PackageSignature Signature { get; private init; }
        public string Error { get; private init; }

        public static PackageVerificationResult Valid(PackageSignature signature) => new() { Status = PackageVerificationStatus.Valid, Signature = signature };
        public static PackageVerificationResult Unsigned() => new() { Status = PackageVerificationStatus.Unsigned, Error = "The package is not signed" };
        public static PackageVerificationResult Invalid(string error) => new() { Status = PackageVerificationStatus.Invalid, Error = error };
    }

    public class PackageVerificationException : Exception
    {
        public PackageVerificationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Verifies plugin and update packages against the keys in <see cref="TrustedPackageKeys"/>. Every package must be verified
    /// before it is extracted or loaded.
    /// </summary>
    public static class PackageVerifier
    {
        private const int MaxSignatureFileSize = 16 * 1024 * 1024;

        /// <summary>
        /// Verifies the zip file: the signature, that every entry is listed with the signed hash and that no entry is missing,
        /// duplicated or unlisted. Also enforces the zip size limits.
        /// </summary>
        /// <param name="expectedType">plugin or update</param>
        /// <param name="expectedName">the expected signed name, null to accept any</param>
        public static PackageVerificationResult VerifyZip(string zipFile, string expectedType, string expectedName = null)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipFile);
                return VerifyZip(archive, expectedType, expectedName);
            }
            catch (InvalidDataException e)
            {
                return PackageVerificationResult.Invalid($"Invalid zip file: {e.Message}");
            }
        }

        public static PackageVerificationResult VerifyZip(ZipArchive archive, string expectedType, string expectedName = null)
        {
            if (archive.Entries.Count > PackageSecurityPolicy.MaxEntries)
            {
                return PackageVerificationResult.Invalid($"Too many entries ({archive.Entries.Count})");
            }

            var signatureEntries = archive.Entries
                .Where(a => PackageSignatureFormat.IsSignatureEntry(PackageSignatureFormat.NormalizePath(a.FullName)))
                .ToList();

            if (signatureEntries.Count == 0)
            {
                // the size limits apply to unsigned packages as well
                var limit = CheckLimits(archive);
                return limit ?? PackageVerificationResult.Unsigned();
            }
            if (signatureEntries.Count > 1)
            {
                return PackageVerificationResult.Invalid("Multiple signature files");
            }

            var signatureEntry = signatureEntries[0];
            if (signatureEntry.Length > MaxSignatureFileSize)
            {
                return PackageVerificationResult.Invalid("Signature file too large");
            }

            PackageSignature signature;
            using (var reader = new StreamReader(signatureEntry.Open()))
            {
                signature = ParseSignature(reader.ReadToEnd(), out var parseError);
                if (signature == null)
                {
                    return PackageVerificationResult.Invalid(parseError);
                }
            }

            var signatureCheck = VerifySignature(signature, expectedType, expectedName);
            if (signatureCheck != null)
            {
                return signatureCheck;
            }

            var expected = signature.Files.ToDictionary(a => a.Path, a => a.Sha256.ToLowerInvariant(), StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;

            foreach (var entry in archive.Entries)
            {
                if (entry == signatureEntry)
                {
                    continue;
                }

                var path = PackageSignatureFormat.NormalizePath(entry.FullName);
                if (PackageSignatureFormat.IsDirectoryEntry(entry))
                {
                    if (entry.Length != 0)
                    {
                        return PackageVerificationResult.Invalid($"Directory entry {path} has content");
                    }
                    continue;
                }

                // case insensitive: on windows two entries that only differ in case are the same file
                if (!seen.Add(path))
                {
                    return PackageVerificationResult.Invalid($"Duplicate entry {path}");
                }

                if (!expected.TryGetValue(path, out var expectedHash))
                {
                    return PackageVerificationResult.Invalid($"Entry {path} is not signed");
                }

                if (entry.CompressedLength > 0 && entry.Length / Math.Max(1, entry.CompressedLength) > PackageSecurityPolicy.MaxCompressionRatio)
                {
                    return PackageVerificationResult.Invalid($"Entry {path} exceeds the compression ratio limit");
                }

                string hash;
                try
                {
                    hash = HashLimited(entry.Open(), ref total);
                }
                catch (PackageVerificationException e)
                {
                    return PackageVerificationResult.Invalid(e.Message);
                }

                if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(hash), Convert.FromHexString(expectedHash)))
                {
                    return PackageVerificationResult.Invalid($"Hash mismatch for {path}");
                }
            }

            var missing = expected.Keys.FirstOrDefault(a => !seen.Contains(a));
            if (missing != null)
            {
                return PackageVerificationResult.Invalid($"Signed entry {missing} is missing");
            }

            return PackageVerificationResult.Valid(signature);
        }

        /// <summary>
        /// Verifies an extracted package (or an installed plugin) directory against the signature file inside it.
        /// </summary>
        /// <param name="directory">the directory that holds the files</param>
        /// <param name="signedPrefix">the path of <paramref name="directory"/> inside the package ("" for the package root, "ComponentName/" for an installed plugin, null: the signed name + "/")</param>
        /// <param name="recursive">check sub directories too</param>
        /// <param name="requireAllSigned">every signed file below <paramref name="signedPrefix"/> must exist</param>
        /// <param name="signatureFile">the signature file, default: the one in <paramref name="directory"/></param>
        public static PackageVerificationResult VerifyDirectory(string directory, string expectedType, string expectedName,
            string signedPrefix, bool recursive, bool requireAllSigned, string signatureFile = null)
        {
            signatureFile ??= Path.Combine(directory, PackageSignatureFormat.SignatureFileName);
            if (!File.Exists(signatureFile))
            {
                return PackageVerificationResult.Unsigned();
            }

            if (new FileInfo(signatureFile).Length > MaxSignatureFileSize)
            {
                return PackageVerificationResult.Invalid("Signature file too large");
            }

            var signature = ParseSignature(File.ReadAllText(signatureFile), out var parseError);
            if (signature == null)
            {
                return PackageVerificationResult.Invalid(parseError);
            }

            var signatureCheck = VerifySignature(signature, expectedType, expectedName);
            if (signatureCheck != null)
            {
                return signatureCheck;
            }

            signedPrefix = PackageSignatureFormat.NormalizePath(signedPrefix ?? signature.Name + "/");
            var expected = signature.Files
                .Where(a => a.Path.StartsWith(signedPrefix, StringComparison.Ordinal))
                .Where(a => recursive || !a.Path.Substring(signedPrefix.Length).Contains('/'))
                .ToDictionary(a => a.Path.Substring(signedPrefix.Length), a => a.Sha256.ToLowerInvariant(), StringComparer.Ordinal);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var fullSignatureFile = Path.GetFullPath(signatureFile);
            long total = 0;

            foreach (var file in Directory.EnumerateFiles(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFullPath(file), fullSignatureFile, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = PackageSignatureFormat.NormalizePath(Path.GetRelativePath(directory, file));
                if (!expected.TryGetValue(relative, out var expectedHash))
                {
                    return PackageVerificationResult.Invalid($"File {relative} is not signed");
                }

                string hash;
                try
                {
                    hash = HashLimited(File.OpenRead(file), ref total);
                }
                catch (PackageVerificationException e)
                {
                    return PackageVerificationResult.Invalid(e.Message);
                }

                if (!string.Equals(hash, expectedHash, StringComparison.Ordinal))
                {
                    return PackageVerificationResult.Invalid($"Hash mismatch for {relative}");
                }

                seen.Add(relative);
            }

            if (requireAllSigned)
            {
                var missing = expected.Keys.FirstOrDefault(a => !seen.Contains(a));
                if (missing != null)
                {
                    return PackageVerificationResult.Invalid($"Signed file {missing} is missing");
                }
            }

            return PackageVerificationResult.Valid(signature);
        }

        /// <summary>
        /// Applies the result to the policy: a valid signature passes, an invalid one throws, an unsigned package throws unless
        /// <see cref="PackageSecurityPolicy.AllowUnsigned"/> is set.
        /// </summary>
        /// <returns>the verified signature, null if an unsigned package was accepted</returns>
        public static PackageSignature EnsureTrusted(PackageVerificationResult result, string package, ILogger logger)
        {
            switch (result.Status)
            {
                case PackageVerificationStatus.Valid:
                    logger?.LogInformation($"Package {package} has a valid signature ({result.Signature.Name} {result.Signature.Version}, key {result.Signature.KeyId})");
                    return result.Signature;
                case PackageVerificationStatus.Unsigned when PackageSecurityPolicy.AllowUnsigned:
                    logger?.LogWarning($"Package {package} is NOT signed, accepted because {PackageSecurityPolicy.AllowUnsignedEnv} is set");
                    return null;
                case PackageVerificationStatus.Unsigned:
                    throw new PackageVerificationException($"Package {package} is not signed (set {PackageSecurityPolicy.AllowUnsignedEnv}=true to accept unsigned packages)");
                default:
                    throw new PackageVerificationException($"Package {package} has an invalid signature: {result.Error}");
            }
        }

        /// <summary>
        /// Anti-rollback: the signed version must not be lower than <paramref name="installedVersion"/>
        /// </summary>
        public static void EnsureNoDowngrade(PackageSignature signature, string installedVersion, string package, ILogger logger)
        {
            if (signature == null || string.IsNullOrEmpty(installedVersion))
            {
                return;
            }

            if (!Version.TryParse(signature.Version, out var signed) || !Version.TryParse(installedVersion, out var installed))
            {
                logger?.LogWarning($"Could not compare the versions of {package} ({signature.Version} / {installedVersion})");
                return;
            }

            if (signed < installed)
            {
                if (PackageSecurityPolicy.AllowDowngrade)
                {
                    logger?.LogWarning($"Downgrade of {package} from {installed} to {signed}, accepted because {PackageSecurityPolicy.AllowDowngradeEnv} is set");
                    return;
                }
                throw new PackageVerificationException($"Package {package} version {signed} is lower than the installed version {installed} (set {PackageSecurityPolicy.AllowDowngradeEnv}=true to allow downgrades)");
            }
        }

        private static PackageVerificationResult CheckLimits(ZipArchive archive)
        {
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                total += entry.Length;
                if (total > PackageSecurityPolicy.MaxUncompressedBytes)
                {
                    return PackageVerificationResult.Invalid("The package exceeds the size limit");
                }
                if (entry.CompressedLength > 0 && entry.Length / Math.Max(1, entry.CompressedLength) > PackageSecurityPolicy.MaxCompressionRatio)
                {
                    return PackageVerificationResult.Invalid($"Entry {entry.FullName} exceeds the compression ratio limit");
                }
            }

            return null;
        }

        internal static PackageSignature ParseSignature(string json, out string error)
        {
            error = null;
            PackageSignature signature;
            try
            {
                signature = JsonConvert.DeserializeObject<PackageSignature>(json);
            }
            catch (JsonException e)
            {
                error = $"Invalid signature file: {e.Message}";
                return null;
            }

            if (signature?.Files == null || string.IsNullOrEmpty(signature.Signature))
            {
                error = "Invalid signature file";
                return null;
            }

            if (signature.FormatVersion != PackageSignatureFormat.FormatVersion || signature.Algorithm != PackageSignatureFormat.Algorithm)
            {
                error = $"Unsupported signature format {signature.FormatVersion}/{signature.Algorithm}";
                return null;
            }

            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in signature.Files)
            {
                if (string.IsNullOrEmpty(file?.Path) || file.Sha256 == null || file.Sha256.Length != 64 ||
                    !file.Sha256.All(Uri.IsHexDigit) || file.Path != PackageSignatureFormat.NormalizePath(file.Path) ||
                    file.Path.Split('/').Any(a => a == ".." || a == ".") || PackageSignatureFormat.IsSignatureEntry(file.Path))
                {
                    error = $"Invalid file entry {file?.Path} in signature";
                    return null;
                }

                if (!paths.Add(file.Path))
                {
                    error = $"Duplicate file entry {file.Path} in signature";
                    return null;
                }
            }

            return signature;
        }

        /// <returns>null if the signature is valid, otherwise the error result</returns>
        private static PackageVerificationResult VerifySignature(PackageSignature signature, string expectedType, string expectedName)
        {
            if (!string.Equals(signature.PackageType, expectedType, StringComparison.Ordinal))
            {
                return PackageVerificationResult.Invalid($"Signed package type {signature.PackageType} does not match {expectedType}");
            }
            if (expectedName != null && !string.Equals(signature.Name, expectedName, StringComparison.Ordinal))
            {
                return PackageVerificationResult.Invalid($"Signed package name {signature.Name} does not match {expectedName}");
            }

            byte[] signatureBytes;
            byte[] payload;
            try
            {
                signatureBytes = Convert.FromBase64String(signature.Signature);
                payload = PackageSignatureFormat.BuildPayload(signature);
            }
            catch (Exception e) when (e is FormatException || e is InvalidDataException)
            {
                return PackageVerificationResult.Invalid($"Invalid signature: {e.Message}");
            }

            foreach (var key in TrustedPackageKeys.Keys)
            {
                if (key.VerifyData(payload, signatureBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                {
                    return null;
                }
            }

            return PackageVerificationResult.Invalid("The signature does not match any trusted key");
        }

        private static string HashLimited(Stream input, ref long total)
        {
            using var stream = input;
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                // count the bytes really read, the zip header sizes can lie
                total += read;
                if (total > PackageSecurityPolicy.MaxUncompressedBytes)
                {
                    throw new PackageVerificationException("The package exceeds the size limit");
                }
                sha.AppendData(buffer, 0, read);
            }

            return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
        }
    }
}
