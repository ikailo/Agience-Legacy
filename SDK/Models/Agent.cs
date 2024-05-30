using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Agent : AgienceObject
    {
        [JsonPropertyName("agency")]
        public Agency? Agency { get; set; }

        [JsonPropertyName("host")]
        public Host? Host { get; set; }

        [JsonIgnore]
        public string? AgencyId
        {
            get { return Agency?.Id; }
            set { Agency = SetId(Agency, value); }
        }
    }
}