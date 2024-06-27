using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Function : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("root_plugin_id")]
        public string? RootPluginId { get; set; }
    }
}
