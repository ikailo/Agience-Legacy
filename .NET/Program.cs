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
            _instance.AddTemplate<PromptOverride>();

            _instance.AgentConnected += _instance_AgentConnected;
            _instance.AgentReady += _instance_AgentReady;

            await _instance.Run();
        }

        private static async Task _instance_AgentConnected(Agent agent)
        {
            agent.Runner.Log($"{agent.Agency.Name} / {agent.Name} Connected");

            // Update default templates TODO: only if allowed
            await agent.Agency.SetTemplateAsDefault<PromptOverride>("prompt");
        }

        private static async Task _instance_AgentReady(Agent agent)
        {
            agent.Runner.Log($"{agent.Name} Ready");

            Data? message = "Ready For Input";

            while (_instance.IsConnected)
            {
                var response = await agent.Runner.Dispatch<InteractWithUser>(message);
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
                _ = await runner.Dispatch("Agience.Agents.Web.Templates.IncomingWebChatMessage", ((string?)output)?.Substring(4));
                //Console.WriteLine(response.Output);
            }

            if (output == "quit")
            {
                Console.WriteLine("Stopping Instance");
                await _instance.Stop();
            }
        }
    }
}