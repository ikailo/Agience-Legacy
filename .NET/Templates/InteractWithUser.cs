using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class InteractWithUser : Template
    {
        public override Data? Description => "Show a message to the user and then receive a text input from the user. Find, and then respond with, the best template response to the user's input.";

        protected override async Task<Data?> Process(Runner runner, Data? input)
        {
            await runner.Dispatch<ShowMessageToUser>($"{input}\r\n> ");

            var inputResponse = await runner.Dispatch<GetInputFromUser>();

            return (await inputResponse.Runner.Prompt(inputResponse.Output)).Output;
        }
    }
}