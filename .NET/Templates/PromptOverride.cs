using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class PromptOverride : Template
    {
        public override Data? Description => "Prompt Override";

        protected override async Task<Data?> Process(Runner runner, Data? input)
        {
            // TODO: Do something with the prompt input
            
            // HERE
            
            _ = await runner.Dispatch<ShowMessageToUser>($"Prompt: {input}");

            return null;
        }
    }
}