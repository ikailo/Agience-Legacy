using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Plugins.Primary._Console
{
    public sealed class ConsolePlugin
    {
        [KernelFunction, Description("Show a message to the person via the console.")]
        public async Task ShowMessageToPerson(
            [FromKernelServices] IConsoleService console,
            [Description("The message to show to the person.")] string message)
        {            
            await console.WriteLineAsync(message);
        }

        [KernelFunction, Description("Get input from the person via the console.")]
        [return: Description("The person's input.")]
        public async Task<string> GetInputFromPerson(
            [FromKernelServices] IConsoleService console)
        {
            return await console.ReadLineAsync() ?? string.Empty;
        }

        [KernelFunction, Description("Interact with the person via the console. Send a message and receive a response.")]
        [return: Description("The person's response.")]
        public async Task<string> InteractWithPerson(
            [FromKernelServices] IConsoleService console,
            [Description("The message to show to the person.")] string message)
        {   
            // TODO: Here we can invoke directly, or invoke through the kernel. Going directly is faster, but going through the kernel could have benefits.
            await ShowMessageToPerson(console, message);
            return await GetInputFromPerson(console);
        }
    }
}