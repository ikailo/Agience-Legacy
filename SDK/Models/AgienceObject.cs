using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class AgienceObject
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        protected static T? SetId<T>(T? obj, string? value) where T : AgienceObject, new()
        {
            if (value == null) return null;
            var result = obj ?? new T();
            result.Id = value;
            return result;
        }
    }
}
