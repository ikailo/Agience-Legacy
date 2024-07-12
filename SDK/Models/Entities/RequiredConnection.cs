using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class RequiredConnection : AgienceEntity
    {
        [JsonPropertyName("connection_id")]
        public string? ConnectionId { get; set; }

        [JsonPropertyName("plugin_id")]
        public string? PluginId { get; set; }

        [JsonPropertyName("function_id")]
        public string? FunctionId { get; set; }        
    }
}

