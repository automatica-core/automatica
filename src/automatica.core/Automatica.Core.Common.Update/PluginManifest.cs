using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Automatica.Core.Common.Update
{
    public class PluginManfiestData
    {
        public PluginManfiestData()
        {
            Resources = new List<string>();
            Dependencies = new List<string>();

            MandatoryLicenseFeatures = new List<string>();
            OptionalLicenseFeatures = new List<string>();
        }

        [JsonProperty("manifest-version")]
        public Version ManifestVersion { get; set; }

        [JsonProperty("plugin-version")]
        public Version PluginVersion { get; set; }

        public Guid PluginGuid { get; set; }

        public string Name { get; set; }
        public string ComponentName { get; set; }

        public string Type { get; set; }

        public string Output { get; set; }
        public List<string> Resources { get; set; }

        public List<string> Dependencies { get; set; }

        public string ProjectPage { get; set; }

        [JsonProperty("MinCoreServerVersion")]
        public Version MinCoreServerVersion { get; set; }

        public List<string> MandatoryLicenseFeatures { get; set; }
        public List<string> OptionalLicenseFeatures { get; set; }
    }
    public class PluginManifest
    {
        public PluginManfiestData Automatica { get; set; }
    }
}
