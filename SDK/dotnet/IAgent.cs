using Microsoft.SemanticKernel;

namespace Agience.Client
{
    public interface IAgent
    {        
        string Id { get; }
        string Name { get; }
        string? Description { get; }
        string? Persona { get; }
        string Timestamp { get; }
        string HostId { get; }
        //string AgencyId { get; }
        Agency Agency { get; }
        Task<IReadOnlyList<ChatMessageContent>> PromptAsync(IReadOnlyList<ChatMessageContent> messages, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatMessageContent>> PromptAsync(string message);
    }
}