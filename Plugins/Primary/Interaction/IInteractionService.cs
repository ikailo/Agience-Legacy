using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agience.Plugins.Primary.Interaction
{   
    public delegate Task AgencyChatHistoryUpdatedHandler(string agencyId, IEnumerable<ChatMessageContent> message);

    public interface IInteractionService
    {   
        public event AgencyChatHistoryUpdatedHandler OnAgencyChatHistoryUpdated;
        Task<IEnumerable<ChatMessageContent>> GetAgencyChatHistoryAsync(string agencyId);
        public Task<bool> InformAgencyAsync(string agencyId, string message);
        public Task<string> PromptAgentAsync(string agentId, string message);
    }
}
