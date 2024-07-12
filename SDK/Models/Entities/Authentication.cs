using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Authentication : AgienceEntity
    {
        [JsonPropertyName("agency_id")]
        public string? AgencyId { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("managed_connection_id")]
        public string? ManagedConnectionId { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; } // TODO: SECURITY: Use a key vault
        
    }
}

