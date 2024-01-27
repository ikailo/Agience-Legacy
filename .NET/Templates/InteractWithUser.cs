using Agience.Client;

namespace Agience.Templates
{
    public class InteractWithUser : Template
    {
        public InteractWithUser()
        {   
            Id = "interact_with_user";
            Description = "Show a message to the user and then receive a text input from the user. Find, and then respond with, the best template response to the user's input.";
        }

        public override Task<bool> Assess(Information information) => Task.FromResult(true);

        public override async Task<Data?> Process(Information information)
        {   
            if (Agent == null) return null; 

            await Agent.Prompt($"{information.Input} \r\n> ", "Show a message to the user.");

            Data? userInput = await Agent.Prompt("Get input from the user");

#if DEBUG

            if (userInput?.Raw?.StartsWith("DEBUG:") ?? false)
            {
                return await Agent.Prompt(userInput, null, "debug");
            }
#endif

            var bestTemplate = await Agent.Prompt(userInput, null, "get_best_template");

            return await Agent.Prompt(userInput, null, bestTemplate?.Structured?["id"] ?? "input_to_output");

        }
    }
}