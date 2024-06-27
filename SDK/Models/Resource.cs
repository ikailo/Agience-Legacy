using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Resource : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("visibility")]
        public Visibility Visibility { get; set; } = Visibility.Private;
    }
}
