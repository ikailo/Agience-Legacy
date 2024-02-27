using Agience.Client;
using Agience.Agents._Console.Templates;

namespace Agience.Agents._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();
        private static readonly Instance _instance = new(_config);

        internal static async Task Main(string[] args)
        {
            _instance.AddTemplate<InteractWithUser>();
            _instance.AddTemplate<ShowMessageToUser>();
            _instance.AddTemplate<GetInputFromUser>(GetInputFromUser_callback);            

            _instance.AgentConnected += _instance_AgentConnected;
            _instance.AgentReady += _instance_AgentReady;

            await _instance.Run();
        }

        private static Task _instance_AgentConnected(Agent agent)
        {
            Console.WriteLine($"{agent.Agency.Name} / {agent.Name} Connected");

            // Update default templates here. TODO: only if allowed            

            return Task.CompletedTask;

        }

        private static async Task _instance_AgentReady(Agent agent)
        {
            Console.WriteLine($"{agent.Name} Ready");

            Data? message = "Ready For Input";

            while (_instance.IsConnected)
            {
                var response = await agent.Runner.DispatchAsync<InteractWithUser>(message);
                message = response.Output;                
            }

            Console.WriteLine($"Instance Stopped");
        }

        private static async Task GetInputFromUser_callback(Runner runner, Data? output)
        {
            if (((string?)output)?.StartsWith("echo:") ?? false)
            {
                var response = await runner.Echo(((string?)output)?.Substring(5));

                Console.WriteLine(response.Output);
            }

            if (((string?)output)?.StartsWith("log:") ?? false)
            {
                runner.Log(((string?)output)?.Substring(4) ?? string.Empty);                
            }

            if (((string?)output)?.StartsWith("web:") ?? false)
            {
                await runner.DispatchAsync("Agience.Agents.Web.Templates.IncomingWebChatMessage", ((string?)output)?.Substring(4));                
            }

            if (output == "quit")
            {
                Console.WriteLine("Stopping Instance");
                await _instance.Stop();
            }
        }
    }
}