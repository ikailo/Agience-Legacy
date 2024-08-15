using Agience.Plugins.Primary._Console;
using Agience.SDK;
using Microsoft.Extensions.Logging;

namespace Agience.Hosts._Console
{
    public class AgienceConsoleService
    {
        private readonly Host _host;
        private readonly ILogger<AgienceConsoleService> _logger;

        private string _contextAgentId = string.Empty;
        private string _contextAgencyId = string.Empty;

        public AgienceConsoleService(Host host, ILogger<AgienceConsoleService> logger)
        {
            _host = host;
            _logger = logger;

            _host.AgentConnected += OnAgentConnected;
            _host.AgencyConnected += OnAgencyConnected;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                if (!string.IsNullOrEmpty(_contextAgentId))
                {
                    Console.Write($"[{_contextAgencyId} - {_contextAgentId}] > ");
                }
                else if (!string.IsNullOrEmpty(_contextAgencyId))
                {
                    Console.Write($"[{_contextAgencyId}] > ");
                }
                else
                {
                    Console.Write("> ");
                }

                string userInput = Console.ReadLine();

                if (!string.IsNullOrEmpty(_contextAgentId))
                {
                    var agent = _host.GetAgentById(_contextAgentId);
                    if (agent != null)
                    {
                        await agent.PromptAsync(userInput);
                    }
                }
                else if (!string.IsNullOrEmpty(_contextAgencyId))
                {
                    var agency = _host.GetAgencyById(_contextAgencyId);
                    if (agency != null)
                    {
                        await agency.PromptAsync(userInput);
                    }
                }
            }
        }

        private async Task OnAgentConnected(Agent agent)
        {
            await ListenToAgentInteractions(agent);
        }

        private async Task OnAgencyConnected(Agency agency)
        {
            await ListenToAgencyInteractions(agency);
        }

        private async Task ListenToAgentInteractions(Agent agent)
        {
            await foreach (var interaction in agent.Interactions)
            {
                Console.WriteLine($"\n[{agent.Id}] Response: {interaction}");
                Console.Write("> ");
            }
        }

        private async Task ListenToAgencyInteractions(Agency agency)
        {
            await foreach (var interaction in agency.Interactions)
            {
                Console.WriteLine($"\n[{agency.Id}] Response: {interaction}");
                Console.Write("> ");
            }
        }
    }
}