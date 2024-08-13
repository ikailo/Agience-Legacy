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

        [JsonPropertyName("agency")]
        public virtual Agency? Agency { get; set; }

        [JsonPropertyName("plugins")]
        public virtual List<Plugin> Plugins { get; set; } = new List<Plugin>();

        [JsonPropertyName("cognitive_function_id")]
        public string? CognitiveFunctionId { get; set; }

        [JsonPropertyName("cognitive_function")]
        public Function? CognitiveFunction { get; set; }
        
        // ***************** //
        // Currently, Agents can be associated only with a single Host.
        // TODO: In the future, an Agency will have mupltiple Hosts and an Agent's Functions can be spread accross all of them.
        [JsonPropertyName("host_id")]
        public string? HostId { get; set; }
        [JsonIgnore]
        public virtual Host? Host { get; set; }
        // ***************** //
    }
}