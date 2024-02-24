using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class InteractWithUser : Template
    {
        public override Data? Description => "Interact with the user. Send them a message and then receive a response.";

        protected override async Task<Data?> Process(Runner runner, Data? input)
        {
            await runner.DispatchAsync<ShowMessageToUser>($"{input}\r\n> ");

            var inputResponse = await runner.DispatchAsync<GetInputFromUser>();

            return (await inputResponse.Runner.Prompt(inputResponse.Output)).Output;
        }
    }
}