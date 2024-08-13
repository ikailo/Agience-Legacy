using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public class CognitiveFunctionChatCompletionService : IChatCompletionService
{
    public IReadOnlyDictionary<string, object?> Attributes => throw new NotImplementedException();

    private readonly KernelFunction _cognitiveFunction;    

    public CognitiveFunctionChatCompletionService(KernelFunction cognitiveFunction)
    {
        _cognitiveFunction = cognitiveFunction;
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {   
        var args = new KernelArguments(executionSettings);        
        
        args["chatHistory"] = chatHistory;
        args["executionSettings"] = executionSettings;

        // TODO: We need to ensure that the cognitive function will return a list of ChatMessageContent
        
        return await _cognitiveFunction.InvokeAsync<IReadOnlyList<ChatMessageContent>>(kernel, args, cancellationToken) ?? new List<ChatMessageContent>();        
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
