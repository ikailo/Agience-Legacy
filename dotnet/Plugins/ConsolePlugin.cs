using Agience.Agents_Console.Plugins;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents._Console.Plugins
{
    public sealed class ConsolePlugin
    {
        [KernelFunction, Description("Show a message to the user via the console.")]
        public void ShowMessageToUser(
            [FromKernelServices] IConsoleService console,
            [Description("The message to show to the user")] string message)
        {
            console.Write(message);
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
            ShowMessageToUser(console, message);
            return await GetInputFromUser(console);
        }
    }
}