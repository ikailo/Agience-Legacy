namespace Agience.Plugins.Primary.Interaction
{
    public interface IInteractionService
    {        
        public delegate void AgencyHistoryUpdatedHandler(string agencyId, string message);
        
        public event AgencyHistoryUpdatedHandler OnAgencyHistoryUpdated;

        IEnumerable<string> GetAgencyHistoryAsync(string agencyId);
        public Task<bool> InformAgencyAsync(string agencyId, string message);
        public Task<string> PromptAgentAsync(string agentId, string message);
    }
}
