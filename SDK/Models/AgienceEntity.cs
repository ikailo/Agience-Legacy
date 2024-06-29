using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class AgienceEntity
    {
        [Key]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
