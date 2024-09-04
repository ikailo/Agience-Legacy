using Microsoft.SemanticKernel;

namespace Agience.Plugins.Primary.Interaction
{   
    public interface IInteractionService
    {   
        public event Func<string, IEnumerable<ChatMessageContent>, Task> AgencyChatHistoryUpdated;
        public event Func<string, Task> AgencyConnected;
        public event Func<string, Task> AgentConnected;
        public event Func<string, Task> AgencyDisconnected;
        public event Func<string, Task> AgentDisconnected;

        Task<IEnumerable<ChatMessageContent>> GetAgencyChatHistoryAsync(string agencyId);
        public Task<bool> InformAgencyAsync(string agencyId, string message);
        public Task<string> PromptAgentAsync(string agentId, string message);
        public Task<bool> IsAgencyConnected(string agencyId);
        public Task<bool> IsAgentConnected(string agentId);

    }
}
