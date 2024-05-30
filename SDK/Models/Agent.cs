using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Agent : AgienceObject
    {
        [JsonPropertyName("agency")]
        public Agency? Agency { get; set; }
        
        [JsonIgnore]
        public string? AgencyId
        {
            get { return Agency?.Id; }
            set { Agency = SetId(Agency, value); }
        }

        [JsonPropertyName("host")]
        public Host? Host { get; set; }
        
        [JsonIgnore]
        public string? HostId
        {
            get { return Host?.Id; }
            set { Host = SetId(Host, value); }
        }
    }
}