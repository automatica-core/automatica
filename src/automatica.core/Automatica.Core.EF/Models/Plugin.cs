using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.EF.Models
{
    public enum PluginType
    {
        Driver,
        Logic
    }
    public class Plugin
    {
        /// <summary>
        /// Gets or Sets ObjId
        /// </summary>
        [JsonProperty(PropertyName = "objId")]
        public Guid? ObjId { get; set; }


        [JsonProperty(PropertyName = "pluginGuid")]
        public Guid? PluginGuid { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [JsonProperty(PropertyName = "componentName")]
        public string ComponentName { get; set; }

        /// <summary>
        /// Gets or Sets Version
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or Sets PluginType
        /// </summary>
        [JsonProperty(PropertyName = "pluginType")]
        public PluginType PluginType { get; set; }

        /// <summary>
        /// Gets or Sets Publisher
        /// </summary>
        [JsonProperty(PropertyName = "publisher")]
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or Sets VersionObj
        /// </summary>
        [NotMapped]
        public Version VersionObj => new Version(Version);

        /// <summary>
        /// Gets or Sets MinCoreServerVersion
        /// </summary>
        [JsonProperty(PropertyName = "minCoreServerVersion")]
        public string MinCoreServerVersion { get; set; }

        /// <summary>
        /// Gets or Sets MinCoreServerVersionObj
        /// </summary>
        [JsonProperty(PropertyName = "minCoreServerVersionObj")]
        [NotMapped]
        public Version MinCoreServerVersionObj
        {
            get
            {
                if(String.IsNullOrEmpty(MinCoreServerVersion))
                {
                    return null;
                }
                return new Version(MinCoreServerVersion);
            }
        }

        /// <summary>
        /// Gets or Sets AzureUrl
        /// </summary>
        [JsonProperty(PropertyName = "azureUrl")]
        public string AzureUrl { get; set; }

        /// <summary>
        /// Gets or Sets AzureFileName
        /// </summary>
        [JsonProperty(PropertyName = "azureFileName")]
        public string AzureFileName { get; set; }

        /// <summary>
        /// Gets or Sets IsPublic
        /// </summary>
        [JsonProperty(PropertyName = "isPublic")]
        public bool? IsPublic { get; set; }

        /// <summary>
        /// Gets or Sets IsPrerelease
        /// </summary>
        [JsonProperty(PropertyName = "isPrerelease")]
        public bool? IsPrerelease { get; set; }

        public bool Loaded { get; set; }
    }
}
