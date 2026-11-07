using System;

namespace Automatica.Core.Common.Update.Signing
{
    /// <summary>
    /// Escape hatches for the package verification, read from environment variables only (not from the database or the web ui).
    /// </summary>
    public static class PackageSecurityPolicy
    {
        /// <summary>
        /// "true": unsigned packages are installed with a warning. A package with an invalid signature is always rejected.
        /// Meant for development and for the transition period until every package in the cloud is signed.
        /// </summary>
        public const string AllowUnsignedEnv = "AUTOMATICA_ALLOW_UNSIGNED_PACKAGES";

        /// <summary>
        /// "true": a package with a lower version than the installed one may be installed (anti-rollback off)
        /// </summary>
        public const string AllowDowngradeEnv = "AUTOMATICA_ALLOW_PACKAGE_DOWNGRADE";

        /// <summary>
        /// "true": installed plugins without a signature file are not loaded (default: loaded with a warning,
        /// plugins installed before package signing have no signature file)
        /// </summary>
        public const string RequireSignedPluginsEnv = "AUTOMATICA_REQUIRE_SIGNED_PLUGINS";

        /// <summary>
        /// Only for unit tests
        /// </summary>
        internal static bool? AllowUnsignedOverride { get; set; }
        internal static bool? AllowDowngradeOverride { get; set; }
        internal static bool? RequireSignedPluginsOverride { get; set; }

        public static bool AllowUnsigned => AllowUnsignedOverride ?? IsTrue(AllowUnsignedEnv);
        public static bool AllowDowngrade => AllowDowngradeOverride ?? IsTrue(AllowDowngradeEnv);
        public static bool RequireSignedPlugins => RequireSignedPluginsOverride ?? IsTrue(RequireSignedPluginsEnv);

        /// <summary>
        /// Maximum number of zip entries in a package
        /// </summary>
        public const int MaxEntries = 20_000;

        /// <summary>
        /// Maximum uncompressed size of a package (zip bomb protection)
        /// </summary>
        public const long MaxUncompressedBytes = 2L * 1024 * 1024 * 1024;

        /// <summary>
        /// Maximum compression ratio of a single entry
        /// </summary>
        public const long MaxCompressionRatio = 200;

        private static bool IsTrue(string env)
        {
            var value = Environment.GetEnvironmentVariable(env);
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
    }
}
