using Agience.Client;
using System.Text;
using System.Text.Json;

namespace Agience.Agents.Primary.Templates.OpenAI
{
    internal class Prompt : Template
    {

        private const string API_KEY = "your_openai_api_key";                
        private const string ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string MODEL = "gpt-3.5-turbo";

        public override Data? Description => "Send a prompt to OpenAI Chat and receive a response.";
        
        protected override async Task<Data?> Process(Runner runner, Data? input = null)
        {
            var data = new
            {
                prompt = input,
                max_tokens = 100
            };

            string jsonData = JsonSerializer.Serialize(data);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");

                using (var content = new StringContent(jsonData, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await client.PostAsync(ENDPOINT, content);
                                        
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();

                        return result;
                    }
                    else
                    {                        
                        return $"Error: {response.StatusCode}";
                    }
                }
            }
        }
    }
}
