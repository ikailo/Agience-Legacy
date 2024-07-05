using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Agience.SDK;
using Host = Agience.SDK.Host;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel;

namespace Agience.Hosts._Console
{
    public class AgienceConsoleHost
    {
        private readonly Host _host;
        private readonly ILogger<AgienceConsoleHost> _logger;
        private readonly IChatCompletionService _chatCompletionService;
        private string? _contextAgentId;

        public AgienceConsoleHost(Host host, ILogger<AgienceConsoleHost> logger, IChatCompletionService chatCompletionService)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
            _host.AgentConnected += _host_AgentConnected;
        }

        public async Task Run()
        {
            await _host.Run();
        }

        private async Task _host_AgentConnected(Agent agent)
        {
            _logger.LogInformation($"{agent.Name} Ready");

            // TODO: Read the input and set the context agent. For now, we will just use the first agent.
            
            if (_contextAgentId == null)
            {
                _contextAgentId = agent.Id;
                _logger.LogInformation($"* Switched context to {_host.GetAgentById(agent.Id)?.Name ?? "Unknown"} *");

                await RunConsole();
            }
        }

        private async Task RunConsole()
        {
            // TODO: Add a way to switch context agents

            var contextAgent = _host.GetAgentById(_contextAgentId);
            if (contextAgent == null) throw new InvalidOperationException("No context agent set");

            string? userInput;
            Console.Write("User > ");

            while ((userInput = Console.ReadLine()) != null)
            {
                await foreach (var message in _host.GetAgentById(_contextAgentId)!.ProcessAsync(userInput))
                {
                    Console.WriteLine($"{message.AuthorRole} > {message.Content}");
                }

                Console.Write("User > ");
            }
        }
    }
}
