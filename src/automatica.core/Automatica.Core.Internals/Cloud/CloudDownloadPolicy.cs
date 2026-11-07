using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Internals.Cloud
{
    /// <summary>
    /// Allow-list for package downloads (plugin and update AzureUrl from the cloud api): https only, and only the cloud host,
    /// azure blob storage or automaticacore.com hosts. More hosts: environment variable AUTOMATICA_DOWNLOAD_HOSTS (comma separated,
    /// "*.example.com" allows sub domains).
    /// </summary>
    public static class CloudDownloadPolicy
    {
        public const string AdditionalHostsEnv = "AUTOMATICA_DOWNLOAD_HOSTS";

        private static readonly string[] DefaultHosts =
        {
            "*.blob.core.windows.net",
            "automaticacore.com",
            "*.automaticacore.com"
        };

        public static bool IsAllowed(string url, string cloudUrl, out string reason)
        {
            reason = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                reason = "the download url is not an absolute url";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                reason = $"the download url must use https (got {uri.Scheme})";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                reason = "the download url must not contain user info";
                return false;
            }

            var hosts = new List<string>(DefaultHosts);
            if (Uri.TryCreate(cloudUrl, UriKind.Absolute, out var cloud))
            {
                hosts.Add(cloud.Host);
            }

            var additional = Environment.GetEnvironmentVariable(AdditionalHostsEnv);
            if (!string.IsNullOrWhiteSpace(additional))
            {
                hosts.AddRange(additional.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            var host = uri.IdnHost.TrimEnd('.');
            if (hosts.Any(a => HostMatches(host, a)))
            {
                return true;
            }

            reason = $"the download host {host} is not allowed";
            return false;
        }

        internal static bool HostMatches(string host, string pattern)
        {
            if (pattern.StartsWith("*."))
            {
                var suffix = pattern.Substring(1); // ".example.com"
                return host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && host.Length > suffix.Length;
            }

            return string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
