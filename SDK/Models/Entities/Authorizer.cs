using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Authorizer : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("manager_id")]
        public string? ManagerId { get; set; }

        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret{ get; set; } // TODO: SECURITY: Use a key vault

        [JsonPropertyName("redirect_uri")]
        public string? RedirectUri { get; set; }

        [JsonPropertyName("token_uri")]
        public string? TokenUri { get; set; }

        [JsonPropertyName("auth_uri")]
        public string? AuthUri { get; set; }
        
        [JsonPropertyName("auth_type")]
        public AuthorizationType? AuthType { get; set; }

        [JsonPropertyName("visibility")]
        public Visibility? Visibility { get; set; } = Entities.Visibility.Private;
    }
}

