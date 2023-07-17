using System.Text.Json.Serialization;

namespace P3.Synology.Api.Client.Apis.Auth.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("is_portal_port")]
        public bool IsPortalPort { get; set; }

        public string Sid { get; set; }
    }
}
