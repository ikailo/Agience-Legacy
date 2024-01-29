using Agience.Templates;
using Agience.Client;

namespace Agience.Agents_Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();

        internal static async Task Main(string[] args)
        {
            var instance = new Instance(_config);

            //instance.LogMessage += LogMessage_callback;

            instance.AgentConnected += Agent_Connected;
            instance.AgencyConnected += Agency_Connected;

            instance.Catalog.Add(agent => new Debug(agent));
            instance.Catalog.Add(() => new InteractWithUser());
            instance.Catalog.Add(() => new GetInputFromUser());
            instance.Catalog.Add(() => new ShowMessageToUser(ShowMessageToUser_callback));

            await instance.Connect();

            /*
            if (_agent != null)
            {
                _agent.Publish("<context setup>");
                _agent.Prompt("Interact with the user.", InteractWithUser_callback);
            }
            */

            do { await Task.Delay(10); } while (instance.IsConnected);
        }

        private static async Task Agent_Connected(Agent agent)
        {
            if (agent.Id == _config.AgentId)
            {
                Console.WriteLine($"{agent.Id} {agent.Name} Connected");
                agent.Invoke<GetInputFromUser>();
            }
        }

        private static async Task Agency_Connected(Agency ageny)
        {
            Console.WriteLine($"{ageny.Id} {ageny.Name} Connected");
        }
        
        private static void LogMessage_callback(object? sender, string message)
        {
            throw new NotImplementedException();
            //Console.WriteLine($"{_agent?.Name ?? "Interaction.Local"} | {message}");
        }

        private async static Task InteractWithUser_callback(Agent agent, Data? userInput)
        {
            if (agent?.Instance == null) { return; }

            if (userInput?.Raw?.Equals("quit", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                await agent.Instance.Disconnect();

                Console.WriteLine($"{agent.Instance.Name} Shut Down");
            }
            else
            {
                agent.Prompt(userInput, "Interact with the user.", InteractWithUser_callback);
            }
        }

        private static void ShowMessageToUser_callback(string? message)
        {
            Console.Write($"{(string.IsNullOrEmpty(message) ? string.Empty : $"{message}")}");
        }
    }
}