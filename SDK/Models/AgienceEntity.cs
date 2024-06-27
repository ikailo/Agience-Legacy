using System.Text.Json.Serialization;

namespace Agience.SDK.Models
{
    public class AgienceEntity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        protected static T SetId<T>(T? entity, string id) where T : AgienceEntity, new()
        {
            entity ??= new T();            
            entity.Id = id;
            return entity;
        }
    }
}
