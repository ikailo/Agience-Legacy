﻿using Microsoft.SemanticKernel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Agience.SDK.Models.Entities
{
    public class Function : AgienceEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("template_format")]
        public TemplateFormat TemplateFormat { get; set; } = TemplateFormat.SemanticKernel;

        [JsonPropertyName("template")]
        public string? Template { get; set; }        

        //[JsonPropertyName("input_variables")]
        //public List<InputVariable> InputVariables { get; set; } = [];

        //[JsonPropertyName("output_variable")]
        //public OutputVariable? OutputVariable { get; set; }

        //[JsonPropertyName("execution_settings")]
        //public Dictionary<string, PromptExecutionSettings> ExecutionSettings { get; set; } = [];
    }

    public enum TemplateFormat
    {
        SemanticKernel
    }
}
