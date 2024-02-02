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
            _instance.AddTemplate<Debug>(Debug_Callback);
            _instance.AddTemplate<ShowMessageToUser>();
            _instance.AddTemplate<GetInputFromUser>();
            _instance.AddTemplate<InteractWithUser>();

            _instance.AgentConnected += _instance_AgentConnected;

            await _instance.Run();
        }

        private static Task _instance_AgentConnected(Agent agent)
        {
            Console.WriteLine($"{agent.Agency?.Name} / {agent.Name} Connected");

            _ = agent.Invoke<InteractWithUser>("Ready for Input").ContinueWith(agent.Invoke(InteractWithUser_callback));

            //var result = await agent.Invoke(typeof(InteractWithUser), "Ready for Input");
            //var result = await agent.Dispatch("Agience.Templates.InteractWithUser", "Ready for Input");
            //var result = await agent.Prompt("Interact with the user.", "Ready for Input");

            return Task.CompletedTask;
        }

        private static async Task InteractWithUser_callback(Agent agent, Data? output)
        {
            while (output?.Raw?.Equals("quit", StringComparison.OrdinalIgnoreCase) == false)
            {
                output = await agent.Invoke<InteractWithUser>(output);
            }

            await _instance.Stop();

            Console.WriteLine($"Instance Stopped");
        }

        private static Task Debug_Callback(Agent agent, Data? output)
        {
            Console.WriteLine($"Debug_Template: {output?.Raw}");

            return Task.CompletedTask;
        }
    }
}