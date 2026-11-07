using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Supervisor.Runtime
{
    public enum SupervisedRole
    {
        Core,
        Satellite
    }

    /// <summary>
    /// What the supervised container gets from the host:
    /// <list type="bullet">
    /// <item>the core: host devices (bind of /dev plus device cgroup rules, <c>deviceCgroupRules</c>, default all
    /// character devices), no docker socket, not privileged</item>
    /// <item>a satellite: the docker socket (it starts the plugin containers) and /dev read-only to find the devices
    /// for them, not privileged</item>
    /// <item>only application settings from the supervisor's environment, never the full environment</item>
    /// </list>
    /// <c>privileged</c>, <c>mountDockerSocket</c>, <c>capAdd</c> and <c>forwardEnv</c> are escape hatches.
    /// </summary>
    public sealed class SupervisedContainerPolicy
    {
        public const string ManagedLabel = "automatica.supervisor.managed";

        private static readonly Regex DigestRegex = new(@"^sha256:[a-f0-9]{64}$", RegexOptions.CultureInvariant);

        // supervisor settings, not meant for the supervised container
        private static readonly string[] SupervisorKeys =
        {
            "image", "imageTag", "imageDigest", "requireDigest", "dockerRegistry", "pollIntervalMinutes", "mounts", "role",
            "privileged", "mountDockerSocket", "deviceCgroupRules", "capAdd", "forwardEnv", "DOCKER_SOCKET",
            "AUTOMATICA_SUPERVISOR_HOSTED"
        };

        private static readonly string[] ForwardedPrefixes =
        {
            "AUTOMATICA_", "MARIADB_", "DATABASE_", "LOG_", "MQTT_", "MATTER_", "ConnectionStrings"
        };

        private static readonly string[] ForwardedNames =
        {
            "TZ", "PRINT_ENV", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT",
            "HTTP_PROXY", "HTTPS_PROXY", "NO_PROXY", "http_proxy", "https_proxy", "no_proxy"
        };

        public SupervisedRole Role { get; }
        public bool Privileged { get; }
        public bool MountDockerSocket { get; }
        public IList<string> DeviceCgroupRules { get; }
        public IList<string> CapAdd { get; }
        public string ImageDigest { get; }
        public bool RequireDigest { get; }
        private readonly string[] _forwardEnv;

        public SupervisedContainerPolicy(IConfiguration config)
        {
            Role = GetRole(config["role"], config["image"]);
            Privileged = IsTrue(config["privileged"]);
            MountDockerSocket = config["mountDockerSocket"] == null ? Role == SupervisedRole.Satellite : IsTrue(config["mountDockerSocket"]);

            var rules = GetList(config, "deviceCgroupRules");
            DeviceCgroupRules = Role == SupervisedRole.Core && rules.Count == 0 && config["deviceCgroupRules"] == null
                ? new List<string> { "c *:* rmw" }
                : rules;
            CapAdd = GetList(config, "capAdd");
            _forwardEnv = GetList(config, "forwardEnv").ToArray();

            ImageDigest = String.IsNullOrWhiteSpace(config["imageDigest"]) ? null : config["imageDigest"].Trim();
            if (ImageDigest != null && !IsValidDigest(ImageDigest))
            {
                throw new ArgumentException($"invalid imageDigest {ImageDigest}");
            }
            RequireDigest = IsTrue(config["requireDigest"]);
        }

        public static SupervisedRole GetRole(string role, string image)
        {
            if (!String.IsNullOrWhiteSpace(role))
            {
                return role.Trim().ToLowerInvariant() switch
                {
                    "satellite" or "slave" => SupervisedRole.Satellite,
                    _ => SupervisedRole.Core
                };
            }

            var name = (image ?? "").ToLowerInvariant();
            return name.Contains("satellite") || name.Contains("slave") ? SupervisedRole.Satellite : SupervisedRole.Core;
        }

        public static bool IsValidDigest(string digest) => digest != null && DigestRegex.IsMatch(digest);

        /// <summary>
        /// Settings-style variables (<c>server:port</c>, <c>db__host</c>), the known application variables and the ones
        /// listed in <c>forwardEnv</c> (a trailing * matches a prefix)
        /// </summary>
        public bool ShouldForward(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            if (_forwardEnv.Any(f => f.EndsWith('*') ? name.StartsWith(f[..^1], StringComparison.Ordinal) : f == name))
            {
                return true;
            }

            var section = name.Split(new[] { ":", "__" }, 2, StringSplitOptions.None)[0];
            if (SupervisorKeys.Contains(section, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return name.Contains(':') || name.Contains("__") ||
                   ForwardedPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)) ||
                   ForwardedNames.Contains(name, StringComparer.Ordinal);
        }

        public IDictionary<string, string> GetForwardedEnvironment(IDictionary environment)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (DictionaryEntry env in environment)
            {
                var key = $"{env.Key}";
                if (ShouldForward(key))
                {
                    result[key] = $"{env.Value}";
                }
            }
            return result;
        }

        private static bool IsTrue(string value) => String.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        private static List<string> GetList(IConfiguration config, string key)
        {
            var children = config.GetSection(key).GetChildren().Select(c => c.Value).Where(v => !String.IsNullOrWhiteSpace(v)).ToList();
            if (children.Count > 0)
            {
                return children;
            }
            var value = config[key];
            return String.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }
}
