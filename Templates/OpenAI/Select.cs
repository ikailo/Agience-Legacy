using Agience.Client;
using System.Text.Json;

namespace Agience.Agents.Primary.Templates.OpenAI
{
    internal class Select : Template
    {
        public override Data? Description => "Select the best template to process the input data.";

        public string OutputFormat => "{template_id:string}";

        protected override async Task<Data?> Process(Runner runner, Data? input = null)
        {
            var persona = "You are an expert decision maker. You are tasked with choosing the best template for processing the input.";

            var responseFormat = $"Respond with a compliant machine-readable JSON document with the following structure: {OutputFormat}.";

            var prompt = "From the JSON list of Template Ids and Descriptions below, select the best one to process the input. " +
                         "Use only the Description field for the selection process. " +
                         "Review each item carefully and consider the input data and the context of the current conversation. " +
                         "If no templates are suitable, use the default: 'Agience.Agents.Primary.Templates.OpenAI.Prompt'.";

            int i = 0;

            var templates = (++i).ToString() + string.Join($"\r\n{++i}. ", runner.GetAvailableTemplates().Select(t => new { t.Description }));

            var context = runner.GetContext();

            var promptInput = new Data
            {
                { "persona", persona },
                { "context", context },
                { "response_format", responseFormat },
                { "prompt",  prompt },                
                { "input",  input }
            };

            var output = (await runner.DispatchAsync<Prompt>($"SYSTEM PERSONA: {persona}\r\nRESPONSE FORMAT: {responseFormat}\r\nCONTEXT: {context}\r\nPROMPT: {prompt}\r\nTEMPLATES: {templates}\r\nINPUT: {input}")).Output;

            return output;
        }
    }
}