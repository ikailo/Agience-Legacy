namespace Agience.SDK.Models.Entities
{
    public interface IAuthorityDataAdapter
    {
        Task<IEnumerable<Plugin>> GetPluginsForHostIdAsync(string hostId);
    }
}
