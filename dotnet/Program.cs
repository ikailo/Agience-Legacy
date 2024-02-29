using Agience.Client;
using Agience.Agents._Console.Templates;

namespace Agience.Agents._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();
        private static readonly Host _host = new(_config);

        internal static async Task Main(string[] args)
        {
            _host.AddTemplate<InteractWithUser>();
            _host.AddTemplate<ShowMessageToUser>();
            _host.AddTemplate<GetInputFromUser>(GetInputFromUser_callback);            

            _host.AgentConnected += _host_AgentConnected;
            _host.AgentReady += _host_AgentReady;

            await _host.Run();
        }

        private static Task _host_AgentConnected(Agent agent)
        {
            Console.WriteLine($"{agent.Agency.Name} / {agent.Name} Connected");

            // Update default templates here. TODO: only if allowed            

            return Task.CompletedTask;

        }

        private static async Task _host_AgentReady(Agent agent)
        {
            Console.WriteLine($"{agent.Name} Ready");

            Data? message = "Ready For Input";

            while (_host.IsConnected)
            {
                var response = await agent.Runner.DispatchAsync<InteractWithUser>(message);
                message = response.Output;                
            }

            Console.WriteLine($"Host Stopped");
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
                Console.WriteLine("Stopping Host");
                await _host.Stop();
            }
        }
    }
}