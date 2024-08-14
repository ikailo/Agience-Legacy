namespace Agience.SDK.Models.Entities
{
    public enum Visibility
    {
        Private,
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

    public enum CompletionAction
    {
        Once,
        Resetart        
    }
}
