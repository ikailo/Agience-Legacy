using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Hosts._Console.Plugins
{
    public sealed class ConsolePlugin
    {
        [KernelFunction, Description("Show a message to the user via the console.")]
        public void ShowMessageToUser(
            [FromKernelServices] IConsoleService console,
            [Description("The message to show to the user")] string message)
        {
            console.WriteLine(message);
        }

        [KernelFunction, Description("Get input from the user via the console.")]
        [return: Description("The user's input.")]
        public async Task<string> GetInputFromUser(
            [FromKernelServices] IConsoleService console)
        {
            return await console.ReadLineAsync() ?? string.Empty;
        }

        [KernelFunction, Description("Interact with the user via the console. Send a message and receive a response.")]
        [return: Description("The user's response.")]
        public async Task<string> InteractWithUser(
            [FromKernelServices] IConsoleService console,
            [Description("The message to show to the user")] string message)
        {
            // TODO: Here we can invoke directly, or invoke through the kernel. Going directly is faster, but going through the kernel could have benefits.
            ShowMessageToUser(console, message);
            return await GetInputFromUser(console);
        }
    }
}