using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class ChatCompletionPlugin
{
    
    private readonly string _apiKey = string.Empty;

    public ChatCompletionPlugin()
    {
        // TODO: Pass in the API key.
    }

    [KernelFunction, Description("Get multiple chat content choices for the prompt and settings.")]
    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        [Description("The chat history to complete.")] ChatHistory chatHistory,
        [Description("The AI execution settings.")] OpenAIPromptExecutionSettings? executionSettings = null
        )
    {
        // This doesn't need to be OpenAIChatCompletionService. It's just a callout to the LLM.
        //var chatCompletionService = new OpenAIChatCompletionService("gpt-3.5-turbo", _apiKey);
        //return await chatCompletionService.GetChatMessageContentsAsync(chatHistory, executionSettings);

        throw new NotImplementedException();
    }
}