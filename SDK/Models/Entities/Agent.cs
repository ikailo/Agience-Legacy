using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Agent : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("agency_id")]
        public string? AgencyId { get; set; }

        [ForeignKey("AgencyId")]
        [JsonIgnore]
        public virtual Agency? Agency { get; set; }
     
    }
}