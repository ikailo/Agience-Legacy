using Agience.Client;
using Agience.Templates;

namespace Agience.Agents_Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();
        private static readonly Instance _instance = new(_config);

        internal static async Task Main(string[] args)
        {
            _instance.AddTemplate<InteractWithUser>();
            _instance.AddTemplate<Agents._Console.Templates.ShowMessageToUser>();
            _instance.AddTemplate<GetInputFromUser>(GetInputFromUser_callback);            

            _instance.AgentConnected += _instance_AgentConnected;

            await _instance.Run();
        }

        private static async Task _instance_AgentConnected(Agent agent)
        {
            Console.WriteLine($"{agent.Agency.Name} / {agent.Name} Connected");

            Data? message = null;            
            
            while (_instance.IsConnected)
            {
                (var runner, message) = await agent.Runner.Dispatch<InteractWithUser>(message ?? "Ready For Input");
            }
            
            Console.WriteLine($"Instance Stopped");
                        
            //var result = await agent.Dispatch("Agience.Templates.InteractWithUser", "Ready for Input");
            //var result = await agent.Prompt("Interact with the user.", "Ready for Input");            
        }

        private static async Task GetInputFromUser_callback(Runner runner, Data? output)
        {  
            if (((string?)output)?.StartsWith("echo:") ?? false)
            {   
                var (echoRunner, echo) = await runner.Dispatch("Agience.Templates.Default.Echo", ((string?)output)?.Substring(5));
                Console.WriteLine(echo);
            }

            if (output == "quit")
            {
                Console.WriteLine("Stopping Instance");
                await _instance.Stop();
            }
        }
    }
}