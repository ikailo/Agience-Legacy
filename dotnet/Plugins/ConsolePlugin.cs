using Agience.Client;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents._Console.Plugins
{
    public sealed class ConsolePlugin
    {
        private readonly StreamReader _inputReader = new(Console.OpenStandardInput());

        [KernelFunction, Description("Get input from the user via the console.")]
        [return: Description("The user's input.")]
        public async Task<Data?> GetInputFromUser()
        {
            return await _inputReader.ReadLineAsync() ?? string.Empty;
        }

        [KernelFunction, Description("Show a message to the user via the console.")]
        public void ShowMessageToUser(
            [Description("The message to show to the user")] Data? message) =>
            Console.Write($"{message}");

        [KernelFunction, Description("Interact with the user via the console. Send them a message and then receive a response.")]
        [return: Description("The user's response.")]
        public async Task<Data?> InteractWithUser(
                       [Description("The message to show to the user")] Data? message)
        {
            ShowMessageToUser(message);
            return await GetInputFromUser();
        }
    }
}