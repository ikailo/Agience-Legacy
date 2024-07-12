using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Connection : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("auth_uri")]
        public string? AuthUri { get; set; }

        [JsonPropertyName("token_uri")]
        public string? TokenUri { get; set; }

        [JsonPropertyName("resource_uri")]
        public string? ResourceUri { get; set; }
    }
}

