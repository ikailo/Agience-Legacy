using Agience.Client;

namespace Agience.Templates
{
    public class InteractWithUser : Template
    {
        public override Data? Description => "Show a message to the user and then receive a text input from the user. Find, and then respond with, the best template response to the user's input.";

        protected override async Task<Data?> Process(Runner runner, Data? input)
        {
            await runner.Dispatch<ShowMessageToUser>($"{input}\r\n> ");

            var (responseRunner, userInput) = await runner.Dispatch<GetInputFromUser>();


            // Possible methods during Processing





            //var baz = await Invoke<InteractWithUser>(input);

            //var bar = await Dispatch("templateId", input);

            //var foo = await Prompt(input);

            /*
#if DEBUG
            
           if (userInput?.Raw?.StartsWith("DEBUG:") ?? false)
            {
                //return await Agent.Prompt(userInput, null, "debug");
            }
#endif
            
            x

            var bestTemplate = await Agent.Prompt(new Data
            {
                { "prompt","Get the ID for the template which is best described by: {input}."},
                { "input", userInput?.Raw ?? string.Empty }
            },
                new string[] { "id" }
            );



            return await Agent.Dispatch(bestTemplate?.Structured?["id"] ?? "Agience.Templates.InputToOutput", userInput);            
       */
            return userInput;
        }
    }
}