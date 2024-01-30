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
            _instance.Catalog.Add<Debug>();
            _instance.Catalog.Add<ShowMessageToUser>();
            _instance.Catalog.Add<GetInputFromUser>();
            _instance.Catalog.Add<InteractWithUser>(InteractWithUser_callback);

            _instance.AgentReady += _instance_AgentReady;

            await _instance.Run();
        }

        private static async Task _instance_AgentReady(Agent agent)
        {
            Console.WriteLine($"{agent.Agency?.Name} / {agent.Name} Ready");

            var result = await agent.Invoke<InteractWithUser>("Ready for Input");
            
            Console.WriteLine("Result: " + result);
        }

        private static async Task<Data?> InteractWithUser_callback(Agent agent, Data? data = null)
        {
            if (data?.Raw?.Equals("quit", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                await _instance.Stop();

                Console.WriteLine($"{_instance.Name} Stopped");
            }
            else
            {
                return await agent.Invoke<InteractWithUser>(data); // TODO: This is recursive. Probably a better way to do this.
            }

            return null;
        }
    }
}