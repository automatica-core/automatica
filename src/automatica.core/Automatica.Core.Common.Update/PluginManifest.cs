using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Newtonsoft.Json.Converters;

namespace Automatica.Core.Common.Update
{
    public class AutomaticaVersion 
    {
        public int Minor { get; set; }
        public int Major { get; set; }
        public int Build { get; set; }
        public int Revision { get; set; }

        public Version ToVersion()
        {
            return new Version(Major, Minor, Build, Revision);
        }

        public static AutomaticaVersion FromVersion(Version version)
        {
            if (version == null)
            {
                return null;
            }
            return new AutomaticaVersion()
            {
                Major = version.Major,
                Minor = version.Minor,
                Build = version.Build,
                Revision = version.Revision
            };
        }
    }

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
        public AutomaticaVersion InternManifestVersion
        {
            get => Common.Update.AutomaticaVersion.FromVersion(ManifestVersion);
            set => ManifestVersion = value.ToVersion();
        }

        [JsonIgnore]
        public Version ManifestVersion { get; set; }

        [JsonProperty("plugin-version")]
        public AutomaticaVersion InternPluginVersion
        {
            get => Common.Update.AutomaticaVersion.FromVersion(PluginVersion);
            set => PluginVersion = value.ToVersion();
        }

        [JsonIgnore]
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
        public AutomaticaVersion InternMinCoreServerVersion
        {
            get => Common.Update.AutomaticaVersion.FromVersion(MinCoreServerVersion);
            set => MinCoreServerVersion = value.ToVersion();
        }

        [JsonIgnore]
        public Version MinCoreServerVersion { get; set; }

        public List<string> MandatoryLicenseFeatures { get; set; }
        public List<string> OptionalLicenseFeatures { get; set; }
    }
    public class PluginManifest
    {
        public PluginManfiestData Automatica { get; set; }
    }
}
