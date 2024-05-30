using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class Plugin : AgienceObject
    {

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("repository_url")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("visibility")]
        public Visibility? Visibility { get; set; }
    }    
}