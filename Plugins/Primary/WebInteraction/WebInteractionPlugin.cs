using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Plugins.Primary.WebInteraction
{
    public class WebInteractionPlugin
    {
        [KernelFunction, Description("Provide a message to the user in the web chat interface.")]
        public async Task IncomingWebChatMessageAsync(            
            [FromKernelServices] IWebInteractionService webInteractionService,
            [Description("The message for the user.")] string message
            )
        {
            await webInteractionService.IncomingWebChatMessageAsync(message);
        }
    }
}