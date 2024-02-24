using Agience.Client;
using System.Text.Json;

namespace Agience.Agents.Primary.Templates.OpenAI
{
    internal class Execute : Template
    {
        public override Data? Description => "Execute a plan from a series of steps.";

        public string InputFormat => "{steps:[string]}";

        protected override async Task<Data?> Process(Runner runner, Data? input = null)
        {
            var inputSteps = input?["steps"];

            var steps = JsonSerializer.Deserialize<string[]>(inputSteps ?? string.Empty);

            if (steps == null) { return "I'm sorry, I wasn't able to read the steps in the plan."; }

            foreach (string step in steps)
            {
                if (string.IsNullOrEmpty(step)) { continue; }

                var select = await runner.DispatchAsync<Select>(step);

                if (select.Output?["template_id"] != null)
                {
                    
                    // TODO: Need to provide the input data in the correct format for the selected template.

                    var execute = await select.Runner.DispatchAsync(select.Output?["template_id"]!, step);

                    runner.AddContext($"SYSTEM: Completed step '{step}' with result '{execute.Output}'.");

                }
            }

            return null;
        }
    }
}
