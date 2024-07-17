using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Credential : AgienceEntity
    {
        [JsonPropertyName("auth_handler_id")]
        public string? AuthHandlerId { get; set; }

        [JsonPropertyName("secret")]
        public string? Secret { get; set; } // TODO: SECURITY: Use a key vault
        
    }
}

