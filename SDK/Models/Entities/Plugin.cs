using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Plugin : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("repository_url")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("visibility")]
        public Visibility Visibility { get; set; } = Visibility.Private;

    }
}