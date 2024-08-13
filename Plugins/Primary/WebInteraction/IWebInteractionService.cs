namespace Agience.Plugins.Primary.WebInteraction
{
    public interface IWebInteractionService
    {   
        public Task IncomingWebChatMessageAsync(string message);
    }
}
