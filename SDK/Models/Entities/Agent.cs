using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Agent : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("agency_id")]
        public string AgencyId { get; set; } = string.Empty;

        [JsonPropertyName("plugins")]
        public virtual List<Plugin> Plugins { get; set; } = new List<Plugin>();

        [JsonPropertyName("agency")]
        public virtual Agency? Agency { get; set; }

        [JsonPropertyName("cognition_function_id")]
        public string CognitiveFunctionId { get; set; } = string.Empty;

        [JsonIgnore]
        public virtual Function? CognitiveFunction { get; set; }

        // ***************** //

        // Agents can be associated with a single Host, currently.
        // TODO: In the future, an Agency will have mupltiple hosts and an Agent will be spread accross all of them.
        [JsonPropertyName("host_id")]
        public string? HostId { get; set; }

        [JsonIgnore]
        public virtual Host? Host { get; set; }

        // ***************** //
    }
}