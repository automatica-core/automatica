using System;
using System.Text;
using System.Runtime.Serialization;
using Automatica.Core.Common.Update;
using Newtonsoft.Json;

namespace Automatica.Core.Internals.Cloud.Model
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
  public class ServerVersion {
    /// <summary>
    /// Gets or Sets ObjId
    /// </summary>
    [DataMember(Name="objId", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "objId")]
    public Guid? ObjId { get; set; }

    /// <summary>
    /// Gets or Sets Version
    /// </summary>
    [DataMember(Name="version", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "version")]
    public string Version { get; set; }

    /// <summary>
    /// Gets or Sets VersionObj
    /// </summary>
    [DataMember(Name="versionObj", EmitDefaultValue=false)]
    [JsonIgnore]
     public Version VersionObj { get; set; }

    [JsonProperty(PropertyName = "versionObj")]
    public AutomaticaVersion InternVersionObj
    {
        get => AutomaticaVersion.FromVersion(VersionObj);
        set => VersionObj = value.ToVersion();
    }

        /// <summary>
        /// Gets or Sets AzureUrl
        /// </summary>
        [DataMember(Name="azureUrl", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "azureUrl")]
    public string AzureUrl { get; set; }

    /// <summary>
    /// Gets or Sets AzureFileName
    /// </summary>
    [DataMember(Name="azureFileName", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "azureFileName")]
    public string AzureFileName { get; set; }

    /// <summary>
    /// Gets or Sets ChangeLog
    /// </summary>
    [DataMember(Name="changeLog", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "changeLog")]
    public string ChangeLog { get; set; }

    /// <summary>
    /// Gets or Sets IsPrerelease
    /// </summary>
    [DataMember(Name="isPrerelease", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "isPrerelease")]
    public bool? IsPrerelease { get; set; }

    /// <summary>
    /// Gets or Sets IsPublic
    /// </summary>
    [DataMember(Name="isPublic", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "isPublic")]
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Gets or Sets Rid
    /// </summary>
    [DataMember(Name="rid", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "rid")]
    public string Rid { get; set; }

    /// <summary>
    /// Gets or Sets IsNewObject
    /// </summary>
    [DataMember(Name="isNewObject", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "isNewObject")]
    public bool? IsNewObject { get; set; }


    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class ServerVersion {\n");
      sb.Append("  ObjId: ").Append(ObjId).Append("\n");
      sb.Append("  Version: ").Append(Version).Append("\n");
      sb.Append("  VersionObj: ").Append(VersionObj).Append("\n");
      sb.Append("  AzureUrl: ").Append(AzureUrl).Append("\n");
      sb.Append("  AzureFileName: ").Append(AzureFileName).Append("\n");
      sb.Append("  ChangeLog: ").Append(ChangeLog).Append("\n");
      sb.Append("  IsPrerelease: ").Append(IsPrerelease).Append("\n");
      sb.Append("  IsPublic: ").Append(IsPublic).Append("\n");
      sb.Append("  Rid: ").Append(Rid).Append("\n");
      sb.Append("  IsNewObject: ").Append(IsNewObject).Append("\n");
      sb.Append("}\n");
      return sb.ToString();
    }

    /// <summary>
    /// Get the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson() {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}
}
