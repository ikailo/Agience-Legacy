using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public enum Visibility
    {
        [JsonPropertyName("private")]
        Private,

        [JsonPropertyName("shared")]
        Shared,

        [JsonPropertyName("public")]
        Public,

        [JsonPropertyName("global")]
        Global
    }
}
