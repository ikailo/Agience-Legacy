using Agience.Templates;
using Agience.Client;

namespace Agience.Agents_Console
{
    internal class Program
    {

        private static Agent? _agent;

        internal static async Task Main(string[] args)
        {
            var _config = new AppConfig();

            var instance = new Instance(_config);

            //instance.LogMessage += LogMessage_callback;

            instance.AgentConnected += Instance_AgentConnected;            

            instance.Catalog.Add(agent => new Debug(agent));
            instance.Catalog.Add(() => new InteractWithUser());
            instance.Catalog.Add(() => new GetInputFromUser());
            //instance.Catalog.Add(() => new ShowMessageToUser(ShowMessageToUser_callback));

            await instance.Connect();

            /*
            // Pick an agent to work with. Here we'll just get one that's defined in the config.
            _agent = instance.Agents.Where(agent => agent.Agency?.Id == _config.AgentId).FirstOrDefault();

            if (_agent != null)
            {
                _agent.Publish("<context setup>");
                _agent.Prompt("Interact with the user.", InteractWithUser_callback);
            }
            */

            do { await Task.Delay(10); } while (instance.IsConnected);
        }

        private static async Task Instance_AgentConnected(Agent agent)
        {
            Console.WriteLine($"{agent.Id} {agent.Name} Connected");
        }

        /*
private static void LogMessage_callback(object? sender, string message)
{
   Console.WriteLine($"{_agent?.Name ?? "Interaction.Local"} | {message}");
}

private async static Task InteractWithUser_callback(Data? userInput)
{
   if (_agent?.Instance == null) { return; }

   if (userInput?.Raw?.Equals("quit", StringComparison.OrdinalIgnoreCase) ?? false)
   {
       await _agent.Instance.Disconnect();

       Console.WriteLine($"{_agent.Instance.Name} Shut Down");
   }
   else
   {
       _agent.Prompt(userInput, "Interact with the user.", InteractWithUser_callback);
   }
}

private static void ShowMessageToUser_callback(string? message)
{
   Console.Write($"{(string.IsNullOrEmpty(message) ? string.Empty : $"{message}")}");
}*/
    }
}