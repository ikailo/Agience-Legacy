namespace Agience.SDK.Models.Entities
{
    public enum Visibility
    {
        Private,
        Shared,
        Public
    }

    public enum AuthorizationType
    {
        None,
        OAuth2,
        ApiKey
    }

    public enum PluginType
    {   
        Curated,
        Compiled
    }
}
