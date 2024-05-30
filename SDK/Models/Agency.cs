using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Agency : AgienceObject
    {
        [JsonPropertyName("agents")]
        public List<Agent> Agents { get; set; } = new();
    }
}

