﻿using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class PluginConnection : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("plugin_id")] 
        public string? PluginId { get; set; }
    }
}

