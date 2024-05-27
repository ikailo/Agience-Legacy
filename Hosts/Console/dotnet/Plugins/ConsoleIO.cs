using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents._Console.Plugins
{
    public interface IConsoleIOService
    {
        Task<string?> ReadLineAsync();
        void Write(string message);
        void WriteLine(string message);
    }

    public sealed class ConsoleIO : IConsoleIOService
    {
        [KernelFunction, Description("Show a message to the user via the console.")]
        public void ShowMessageToUser(
            [FromKernelServices] IConsoleIOService console,
            [Description("The message to show to the user")] string message)
        {            
            console.WriteLine(message);
        }

        [KernelFunction, Description("Get input from the user via the console.")]
        [return: Description("The user's input.")]
        public async Task<string> GetInputFromUser(
            [FromKernelServices] IConsoleIOService console)
        {
            return await console.ReadLineAsync() ?? string.Empty;
        }

        [KernelFunction, Description("Interact with the user via the console. Send a message and receive a response.")]
        [return: Description("The user's response.")]
        public async Task<string> InteractWithUser(
            [FromKernelServices] IConsoleIOService console,
            [Description("The message to show to the user")] string message)
        {   
            // TODO: Here we can invoke directly, or invoke through the kernel. Going directly is faster, but going through the kernel could have benefits.
            ShowMessageToUser(console, message);
            return await GetInputFromUser(console);
        }

        private readonly StreamReader _inputReader = new(Console.OpenStandardInput());

        public Task<string?> ReadLineAsync()
        {
            return _inputReader.ReadLineAsync();
        }

        public void Write(string message)
        {
            Console.Write(message);
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

    }
}