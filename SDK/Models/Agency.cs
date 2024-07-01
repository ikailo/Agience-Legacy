using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Agency : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonIgnore]
        public virtual List<Agent> Agents { get; set; } = new List<Agent>();
    }
}

