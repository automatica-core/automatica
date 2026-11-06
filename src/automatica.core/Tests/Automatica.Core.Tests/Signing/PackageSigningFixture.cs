using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Automatica.Core.Common.Update.Signing;
using Xunit;

namespace Automatica.Core.Tests.Signing
{
    /// <summary>
    /// The trusted keys and the policy are static: every test that changes them runs in this collection, never in parallel
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class PackageSigningCollection : ICollectionFixture<PackageSigningFixture>
    {
        public const string Name = "PackageSigning";
    }

    public sealed class PackageSigningFixture : IDisposable
    {
        public ECDsa TrustedKey { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public ECDsa UntrustedKey { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        public PackageSigningFixture()
        {
            TrustedPackageKeys.OverrideKeys = new List<ECDsa> { TrustedKey };
        }

        public static void ResetPolicy()
        {
            PackageSecurityPolicy.AllowUnsignedOverride = false;
            PackageSecurityPolicy.AllowDowngradeOverride = false;
            PackageSecurityPolicy.RequireSignedPluginsOverride = false;
        }

        public void Dispose()
        {
            TrustedPackageKeys.OverrideKeys = null;
            PackageSecurityPolicy.AllowUnsignedOverride = null;
            PackageSecurityPolicy.AllowDowngradeOverride = null;
            PackageSecurityPolicy.RequireSignedPluginsOverride = null;
        }

        public static string CreateZip(IDictionary<string, string> entries)
        {
            var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
            using var archive = ZipFile.Open(file, ZipArchiveMode.Create);
            foreach (var entry in entries)
            {
                var zipEntry = archive.CreateEntry(entry.Key);
                using var writer = new StreamWriter(zipEntry.Open());
                writer.Write(entry.Value);
            }

            return file;
        }

        public static string CreatePluginZip(string componentName = "TestDriver")
        {
            return CreateZip(new Dictionary<string, string>
            {
                { "automatica-manifest.json", "{}" },
                { $"{componentName}/{componentName}.dll", "driver binary" },
                { $"{componentName}/Dependency.dll", "dependency binary" }
            });
        }
    }
}
