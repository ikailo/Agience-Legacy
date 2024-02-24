using Agience.Agents.Primary.Templates.OpenAI;
using Agience.Client;
using System.Text.Json;

namespace Agience.Agents.Primary.Templates.Process
{
    internal class Input : Template
    {
        public override Data? Description => "Handle the user's input.";

        protected override async Task<Data?> Process(Runner runner, Data? input = null) 
        {
            runner.AddContext($"USER: {input}");

            // Make a plan
            var plan = await runner.DispatchAsync<Plan>(input);

            runner.AddContext($"SYSTEM: Created Plan - {string.Join("; ", JsonSerializer.Deserialize<string[]>(plan.Output?["steps"] ?? string.Empty) ?? Array.Empty<string>())}");

            // Execute the plan
            var execute = await plan.Runner.DispatchAsync<Execute>(plan.Output);

            runner.AddContext($"SYSTEM: Executed plan with output '{execute.Output}'");

            return execute.Output;
        }
    }
}