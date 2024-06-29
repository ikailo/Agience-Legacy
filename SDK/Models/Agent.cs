using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Agent : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("agency_id")]
        public string? AgencyId { get; set; }

        [JsonPropertyName("plugin_id")]
        public string? PluginId { get; set; }

        [ForeignKey("AgencyId")]
        [JsonIgnore]
        public virtual Agency? Agency { get; set; }

        [ForeignKey("PluginId")]
        [JsonIgnore]
        public virtual Plugin? Plugin { get; set; }
    }
}