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
        public virtual Agency Agency { get; set; } = default!;

        [JsonPropertyName("plugins")]
        public virtual List<Plugin> Plugins { get; set; } = new List<Plugin>();

        [JsonPropertyName("chat_completion_function_name")]
        public string? ChatCompletionFunctionName { get; set; }

        [JsonPropertyName("auto_start_function_name")]
        public string? AutoStartFunctionName { get; set; }
        
        [JsonPropertyName("auto_start_function_completion_action")]
        public CompletionAction? AutoStartFunctionCompletionAction { get; set; }
                
        //[JsonPropertyName("prompt_function_name")]        
        //public string? PromptFunctionName { get; set; }

        //[JsonPropertyName("agency_subscribe_function_name")]
        //public string? AgencySubscribeFunctionName { get; set; }

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