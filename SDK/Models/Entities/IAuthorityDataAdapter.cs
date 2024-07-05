namespace Agience.SDK.Models.Entities
{
    public interface IAuthorityDataAdapter
    {
        Task<IEnumerable<Agent>> GetAgentsForHostIdAsync(string hostId);
        Task<IEnumerable<Plugin>> GetPluginsForHostIdAsync(string hostId);
    }
}
