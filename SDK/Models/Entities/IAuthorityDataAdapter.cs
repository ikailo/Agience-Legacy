namespace Agience.SDK.Models.Entities
{
    public interface IAuthorityDataAdapter
    {
        Task<IEnumerable<Agent>> GetAgentsForHostIdNoTrackingAsync(string hostId);
        Task<IEnumerable<Plugin>> GetPluginsForHostIdNoTrackingAsync(string hostId);
    }
}
