using Agience.Client;

namespace Agience.Templates
{
    public class InteractWithUser : Template
    {
        public InteractWithUser()
        {   
            Description = "Show a message to the user and then receive a text input from the user. Find, and then respond with, the best template response to the user's input.";
        }

        public override async Task<Data?> Process(Data? data)
        {   
            await Agent.Invoke<ShowMessageToUser>($"{data}\r\n>");

            var userInput = await Agent.Invoke<GetInputFromUser>();

            /*
#if DEBUG
            
           if (userInput?.Raw?.StartsWith("DEBUG:") ?? false)
            {
                //return await Agent.Prompt(userInput, null, "debug");
            }
#endif
            */

            var bestTemplate = await Agent.Prompt("Find the best template.", userInput);

            return await Agent.Dispatch(bestTemplate?.Structured?["id"] ?? "Agience.Templates.InputToOutput", userInput);            
        }
    }
}