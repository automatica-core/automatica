using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Automatica.Core.Satellite.Abstraction;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Satellite.Runtime
{
    /// <summary>
    /// What a start request from the core may ask the satellite to run:
    /// <list type="bullet">
    /// <item>only images from allow-listed repositories (<c>docker:allowedImages</c>, default <c>automaticacore/*</c>),
    /// pulled by name and tag or pinned by digest; image imports from a URL (<c>ImageSource</c>) are rejected</item>
    /// <item>no privileged mode; host devices only from the allow-list (<c>docker:allowedDevices</c>), by default every
    /// existing serial/USB/GPIO device on it (<c>docker:devices=auto</c>) so today's drivers keep working</item>
    /// <item>added capabilities only from <c>docker:allowedCapabilities</c> (default none)</item>
    /// </list>
    /// </summary>
    public sealed class ContainerPolicy
    {
        public const string ManagedLabel = "automatica.satellite.managed";
        public const string SatelliteLabel = "automatica.satellite.id";
        public const string NodeLabel = "automatica.satellite.node";

        public const string DefaultAllowedImages = "automaticacore/*";

        public const string DefaultAllowedDevices =
            "/dev/ttyUSB*,/dev/ttyACM*,/dev/ttyAMA*,/dev/ttyS*,/dev/ttyMFD*,/dev/serial/by-id/*,/dev/bus/usb," +
            "/dev/gpiomem,/dev/gpiochip*,/dev/i2c-*,/dev/spidev*,/dev/hidraw*";

        /// <summary>
        /// Older cores send this in every request, it is ignored
        /// </summary>
        private const string LegacyImageSource = "https://hub.docker.com";

        private static readonly Regex RepositoryRegex = new(
            @"^(?:(?<host>[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?(?::[0-9]{1,5})?)/)?(?<path>[a-z0-9]+(?:(?:[._]|__|-+)[a-z0-9]+)*(?:/[a-z0-9]+(?:(?:[._]|__|-+)[a-z0-9]+)*)*)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex TagRegex = new(@"^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$", RegexOptions.CultureInvariant);
        private static readonly Regex DigestRegex = new(@"^sha256:[a-f0-9]{64}$", RegexOptions.CultureInvariant);
        private static readonly Regex CapabilityRegex = new(@"^[A-Z_]{2,32}$", RegexOptions.CultureInvariant);

        private static readonly string[] DockerHubHosts = { "docker.io", "index.docker.io", "registry-1.docker.io" };

        private readonly string[] _allowedImages;
        private readonly string[] _allowedDevices;
        private readonly HashSet<string> _allowedCapabilities;
        private readonly bool _autoDevices;
        private readonly Func<string, IEnumerable<string>> _expandDeviceGlob;

        public bool Privileged { get; }

        public ContainerPolicy(IConfiguration config, Func<string, IEnumerable<string>> expandDeviceGlob = null)
            : this(config["docker:allowedImages"], config["docker:allowedDevices"], config["docker:allowedCapabilities"],
                config["docker:devices"], String.Equals(config["docker:privileged"], "true", StringComparison.OrdinalIgnoreCase), expandDeviceGlob)
        {
        }

        public ContainerPolicy(string allowedImages, string allowedDevices = null, string allowedCapabilities = null,
            string devices = null, bool privileged = false, Func<string, IEnumerable<string>> expandDeviceGlob = null)
        {
            _allowedImages = Split(String.IsNullOrWhiteSpace(allowedImages) ? DefaultAllowedImages : allowedImages);
            _allowedDevices = Split(allowedDevices ?? DefaultAllowedDevices);
            _allowedCapabilities = new HashSet<string>(Split(allowedCapabilities ?? "").Select(NormalizeCapability), StringComparer.Ordinal);
            _autoDevices = String.IsNullOrWhiteSpace(devices) || devices.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase);
            Privileged = privileged;
            _expandDeviceGlob = expandDeviceGlob ?? ExpandGlob;
        }

        private static string[] Split(string value)
        {
            return value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static string NormalizeCapability(string cap)
        {
            cap = cap.ToUpperInvariant();
            return cap.StartsWith("CAP_", StringComparison.Ordinal) ? cap[4..] : cap;
        }

        /// <summary>
        /// Checks the image of a start request; <paramref name="imageReference"/> is what to pull and run
        /// (<c>repo:tag</c> or <c>repo@sha256:...</c>)
        /// </summary>
        public bool TryGetImage(ActionRequest request, string tagOverride, out string repository, out string tag, out string imageReference, out string error)
        {
            repository = null;
            tag = null;
            imageReference = null;
            error = null;

            if (!String.IsNullOrWhiteSpace(request.ImageSource) &&
                !String.Equals(request.ImageSource.TrimEnd('/'), LegacyImageSource, StringComparison.OrdinalIgnoreCase))
            {
                error = $"image sources other than a registry are not allowed ({request.ImageSource})";
                return false;
            }

            if (!IsAllowedImage(request.ImageName, out repository, out error))
            {
                return false;
            }

            tag = String.IsNullOrEmpty(tagOverride) ? request.Tag : tagOverride;
            if (String.IsNullOrEmpty(tag))
            {
                tag = "latest";
            }
            if (!TagRegex.IsMatch(tag))
            {
                error = $"invalid tag {tag}";
                return false;
            }

            if (!String.IsNullOrEmpty(request.ImageDigest))
            {
                if (!DigestRegex.IsMatch(request.ImageDigest))
                {
                    error = $"invalid image digest {request.ImageDigest}";
                    return false;
                }
                imageReference = $"{repository}@{request.ImageDigest}";
            }
            else
            {
                imageReference = $"{repository}:{tag}";
            }
            return true;
        }

        /// <summary>
        /// Repository name in the allow-list. Patterns: an exact name, or <c>namespace/*</c> for all repositories of a
        /// namespace. Docker Hub host prefixes are removed; other registries must be named in the pattern.
        /// </summary>
        public bool IsAllowedImage(string imageName, out string repository, out string error)
        {
            repository = null;
            error = null;

            if (String.IsNullOrWhiteSpace(imageName) || imageName.Contains('@') || imageName.Length > 255)
            {
                error = $"invalid image name {imageName}";
                return false;
            }

            var match = RepositoryRegex.Match(imageName);
            if (!match.Success)
            {
                error = $"invalid image name {imageName}";
                return false;
            }

            var host = match.Groups["host"].Success ? match.Groups["host"].Value : null;
            var path = match.Groups["path"].Value;

            // "automaticacore/x" has no host; a first component without a dot, colon or "localhost" is a namespace
            if (host != null && !host.Contains('.') && !host.Contains(':') && host != "localhost")
            {
                path = $"{host}/{path}";
                host = null;
            }
            host = host?.ToLowerInvariant();
            if (host != null && DockerHubHosts.Contains(host))
            {
                host = null;
            }
            if (host == null && path.StartsWith("library/", StringComparison.Ordinal))
            {
                path = path["library/".Length..];
            }

            var normalized = host == null ? path : $"{host}/{path}";

            foreach (var pattern in _allowedImages)
            {
                if (pattern.EndsWith("/*", StringComparison.Ordinal))
                {
                    var prefix = pattern[..^1];
                    var rest = normalized.StartsWith(prefix, StringComparison.Ordinal) ? normalized[prefix.Length..] : null;
                    if (!String.IsNullOrEmpty(rest) && !rest.Contains('/'))
                    {
                        repository = normalized;
                        return true;
                    }
                }
                else if (String.Equals(pattern, normalized, StringComparison.Ordinal))
                {
                    repository = normalized;
                    return true;
                }
            }

            error = $"image {imageName} is not in the allowed repositories ({String.Join(", ", _allowedImages)})";
            return false;
        }

        public bool IsAllowedDevice(string device)
        {
            if (String.IsNullOrWhiteSpace(device) || !device.StartsWith("/dev/", StringComparison.Ordinal) || device.Contains(".."))
            {
                return false;
            }
            return _allowedDevices.Any(pattern => GlobMatch(pattern, device));
        }

        /// <summary>
        /// Host devices for the container: the requested ones that are allowed (and exist), plus with
        /// <c>docker:devices=auto</c> every existing device matching the allow-list
        /// </summary>
        public IList<string> GetDevices(ActionRequest request, Action<string> rejected = null)
        {
            var result = new List<string>();

            foreach (var device in request.Devices ?? Array.Empty<string>())
            {
                if (IsAllowedDevice(device))
                {
                    result.AddRange(_expandDeviceGlob(device));
                }
                else
                {
                    rejected?.Invoke(device);
                }
            }

            if (_autoDevices)
            {
                foreach (var pattern in _allowedDevices)
                {
                    result.AddRange(_expandDeviceGlob(pattern));
                }
            }

            return result.Distinct(StringComparer.Ordinal).ToList();
        }

        public IList<string> GetCapabilities(ActionRequest request, Action<string> rejected = null)
        {
            var result = new List<string>();
            foreach (var cap in request.CapAdd ?? Array.Empty<string>())
            {
                var normalized = NormalizeCapability(cap ?? "");
                if (CapabilityRegex.IsMatch(normalized) && _allowedCapabilities.Contains(normalized))
                {
                    result.Add(normalized);
                }
                else
                {
                    rejected?.Invoke(cap);
                }
            }
            return result.Distinct().ToList();
        }

        public static bool IsValidDigest(string digest) => digest != null && DigestRegex.IsMatch(digest);

        internal static bool GlobMatch(string pattern, string value)
        {
            var regex = "^" + Regex.Escape(pattern).Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]") + "$";
            return Regex.IsMatch(value, regex, RegexOptions.CultureInvariant);
        }

        private static IEnumerable<string> ExpandGlob(string pattern)
        {
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                return File.Exists(pattern) || Directory.Exists(pattern) ? new[] { pattern } : Array.Empty<string>();
            }

            var dir = Path.GetDirectoryName(pattern);
            var filePattern = Path.GetFileName(pattern);
            if (dir == null || dir.Contains('*') || dir.Contains('?') || !Directory.Exists(dir))
            {
                return Array.Empty<string>();
            }

            try
            {
                return Directory.EnumerateFileSystemEntries(dir, filePattern).Select(a => a.Replace('\\', '/')).ToList();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }
    }
}
