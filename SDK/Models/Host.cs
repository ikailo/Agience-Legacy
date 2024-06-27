using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Host : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}