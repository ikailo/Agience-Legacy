using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class ManagedConnection : AgienceEntity
    {
        [JsonPropertyName("manager_id")]
        public string? ManagerId { get; set; }

        [JsonPropertyName("connection_id")]
        public string? ConnectionId { get; set; }

        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret{ get; set; } // TODO: SECURITY: Use a key vault

        [JsonPropertyName("redirect_uri")]
        public string? RedirectUri { get; set; }
    }
}

