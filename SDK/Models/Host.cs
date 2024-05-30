using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Host : AgienceObject
    {
        [JsonPropertyName("plugin")]
        public Plugin? Plugin { get; set; }
        
        [JsonPropertyName("agents")]
        public List<Agent> Agents { get; set; } = new();        

        [JsonIgnore]
        public string? PluginId
        {
            get { return Plugin?.Id; }
            set { Plugin = SetId(Plugin, value); }
        }
    }
}