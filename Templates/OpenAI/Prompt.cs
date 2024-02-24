using Agience.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agience.Agents.Primary.Templates.OpenAI
{
    internal class Prompt : Template
    {
        private const string API_KEY = "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"; // TODO: Get from config/authority
        private const string ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string MODEL = "gpt-3.5-turbo";
        
        private readonly HttpClient _httpClient;

        public Prompt()
        {
            // TODO: Get reusable HttpClient from Runner or Agency
            _httpClient = new HttpClient();
        }

        public override Data? Description => "Send a prompt to ChatGPT and receive a response.";

        protected override async Task<Data?> Process(Runner runner, Data? input = null)
        {
            var request = new Request
            {
                Model = MODEL,
                Messages = new List<Message>
                {
                    //new Message { Role = "system", Content = "You are a helpful assistant." },
                    new Message { Role = "user", Content = input }
                }
            };

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ENDPOINT, content);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ChatCompletionResponse>(await response.Content.ReadAsStringAsync());

                return result?.Choices?[0]?.Message?.Content;
            }
            else
            {
                return $"Error: {response.StatusCode}";
            }
        }
    }

    internal class Request
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("messages")]
        public List<Message>? Messages { get; set; }
    }

    internal class Message
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    internal class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    internal class Choice
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("message")]
        public Message? Message { get; set; }

        [JsonPropertyName("logprobs")]
        public string? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    internal class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }
    }
}
