namespace Agience.SDK.Models.Entities
{
    public interface IHostDataAdapter
    {
        Task<IEnumerable<Agent>> GetAgentsForHostIdAsync(string hostId);
    }
}
