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
            await agent.Runner.Log($"{agent.Agency.Name} / {agent.Name} Connected");

            // Update default templates TODO: only if allowed
            agent.Agency.SetDefaultTemplate<PromptOverride>("prompt");
        }

        private static async Task _instance_AgentReady(Agent agent)
        {
            await agent.Runner.Log($"{agent.Name} Ready");

            Data? message = null;            
            
            while (_instance.IsConnected)
            {
                (var runner, message) = await agent.Runner.Dispatch<InteractWithUser>(message ?? "Ready For Input");
            }
            
            Console.WriteLine($"Instance Stopped"); 
        }

        private static async Task GetInputFromUser_callback(Runner runner, Data? output)
        {  
            if (((string?)output)?.StartsWith("echo:") ?? false)
            {   
                var (echoRunner, echo) = await runner.Echo(((string?)output)?.Substring(5));

                Console.WriteLine(echo);
            }

            if (((string?)output)?.StartsWith("web:") ?? false)
            {
                var (webRunner, webResponse) = await runner.Dispatch("Agience.Agents.Web.Templates.IncomingWebChatMessage", ((string?)output)?.Substring(4));
                Console.WriteLine(webResponse);
            }

            if (output == "quit")
            {
                Console.WriteLine("Stopping Instance");
                await _instance.Stop();
            }
        }
    }
}