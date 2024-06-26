using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Host : AgienceRecord
    {

        [JsonPropertyName("plugin")]
        public Plugin? Plugin { get; set; }

        [JsonIgnore]
        public string? PluginId
        {
            get { return Plugin?.Id; }
            set { Plugin = SetId(Plugin, value); }
        }

    }
}