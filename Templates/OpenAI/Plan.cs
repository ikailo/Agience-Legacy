using Agience.Client;

namespace Agience.Agents.Primary.Templates.OpenAI
{
    internal class Plan : Template
    {
        public override Data? Description => "Create a plan that includes a series of steps designed to correctly respond to and fulfill the user's input and request.";

        public string OutputFormat => "{steps:[string]}";

        protected override async Task<Data?> Process(Runner runner, Data? input = null)
        {
            var persona = "You are an expert planner.";

            var responseFormat = $"Respond with a compliant machine-readable JSON document with the following structure: {OutputFormat}. Do not prefix any step with numbers or bullet points.";

            var prompt = $"Create a plan that describes a series of steps designed to correctly respond to and fulfill the user's input and request.";

            var output = (await runner.DispatchAsync<Prompt>($"SYSTEM PERSONA: {persona}\r\nRESPONSE FORMAT: {responseFormat}\r\nPROMPT: {prompt}\r\nUSER INPUT: {input}")).Output;

            return output;
        }
    }
}
