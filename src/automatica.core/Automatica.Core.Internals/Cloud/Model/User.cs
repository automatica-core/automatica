using System;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Automatica.Core.Internals.Cloud.Model
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
  public class User {
    /// <summary>
    /// Gets or Sets ObjId
    /// </summary>
    [DataMember(Name="objId", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "objId")]
    public Guid? ObjId { get; set; }

    /// <summary>
    /// Gets or Sets UserName
    /// </summary>
    [DataMember(Name="userName", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "userName")]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or Sets Email
    /// </summary>
    [DataMember(Name="email", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "email")]
    public string Email { get; set; }

    /// <summary>
    /// Gets or Sets PublisherName
    /// </summary>
    [DataMember(Name="publisherName", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "publisherName")]
    public string PublisherName { get; set; }

    /// <summary>
    /// Gets or Sets PasswordHash
    /// </summary>
    [DataMember(Name="passwordHash", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "passwordHash")]
    public string PasswordHash { get; set; }

    /// <summary>
    /// Gets or Sets Salt
    /// </summary>
    [DataMember(Name="salt", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "salt")]
    public string Salt { get; set; }

    /// <summary>
    /// Gets or Sets FirstName
    /// </summary>
    [DataMember(Name="firstName", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "firstName")]
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or Sets LastName
    /// </summary>
    [DataMember(Name="lastName", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "lastName")]
    public string LastName { get; set; }

    /// <summary>
    /// Gets or Sets Enabled
    /// </summary>
    [DataMember(Name="enabled", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or Sets ActivationCode
    /// </summary>
    [DataMember(Name="activationCode", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "activationCode")]
    public string ActivationCode { get; set; }

    /// <summary>
    /// Gets or Sets UserRole
    /// </summary>
    [DataMember(Name="userRole", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "userRole")]
    public int? UserRole { get; set; }

    /// <summary>
    /// Gets or Sets ApiKey
    /// </summary>
    [DataMember(Name="apiKey", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "apiKey")]
    public Guid? ApiKey { get; set; }

    /// <summary>
    /// Gets or Sets Token
    /// </summary>
    [DataMember(Name="token", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "token")]
    public string Token { get; set; }


    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class User {\n");
      sb.Append("  ObjId: ").Append(ObjId).Append("\n");
      sb.Append("  UserName: ").Append(UserName).Append("\n");
      sb.Append("  Email: ").Append(Email).Append("\n");
      sb.Append("  PublisherName: ").Append(PublisherName).Append("\n");
      sb.Append("  PasswordHash: ").Append(PasswordHash).Append("\n");
      sb.Append("  Salt: ").Append(Salt).Append("\n");
      sb.Append("  FirstName: ").Append(FirstName).Append("\n");
      sb.Append("  LastName: ").Append(LastName).Append("\n");
      sb.Append("  Enabled: ").Append(Enabled).Append("\n");
      sb.Append("  ActivationCode: ").Append(ActivationCode).Append("\n");
      sb.Append("  UserRole: ").Append(UserRole).Append("\n");
      sb.Append("  ApiKey: ").Append(ApiKey).Append("\n");
      sb.Append("  Token: ").Append(Token).Append("\n");
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
