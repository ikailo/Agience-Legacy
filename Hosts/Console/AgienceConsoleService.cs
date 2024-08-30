using Agience.SDK;
using Microsoft.Extensions.Logging;
using MQTTnet.Internal;
using System.Collections.Concurrent;

namespace Agience.Hosts._Console
{
    public class AgienceConsoleService
    {
        private readonly Host _host;
        private readonly ILogger<AgienceConsoleService> _logger;
        private string? _currentAgentId;
        private string? _currentAgencyId;
        private readonly Dictionary<string, Agent> _agents = new();
        private readonly Dictionary<string, Agency> _agencies = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingAgentPrompts = new();
        private bool _isScrollingEnabled = true;

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
                DisplayPrompt();

                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (input.StartsWith("/"))
                {
                    await ProcessCommand(input);
                }
                else
                {
                    await ProcessInput(input);
                }
            }
        }

        private async Task DisplayPrompt()
        {
            string prompt = "> ";

            if (!string.IsNullOrEmpty(_currentAgentId) && _agents.TryGetValue(_currentAgentId, out var agent))
            {
                prompt = $"{GetNotifications()}\\{agent.Agency.Name}\\{agent.Name}> ";
            }
            else if (!string.IsNullOrEmpty(_currentAgencyId) && _agencies.TryGetValue(_currentAgencyId, out var agency))
            {
                prompt = $"{GetNotifications()}\\{agency.Name}> ";
            }

            Console.Write(prompt);
        }

        private string GetNotifications()
        {
            // Placeholder: Replace with actual notification count retrieval logic
            return "0";
        }

        private async Task ProcessCommand(string command)
        {
            switch (command.ToLower())
            {
                case "/scroll":
                    _isScrollingEnabled = true;
                    break;
                case "/notify":
                    _isScrollingEnabled = false;
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private async Task ProcessInput(string input)
        {
            if (!string.IsNullOrEmpty(_currentAgentId) && _agents.TryGetValue(_currentAgentId, out var agent))
            {
                var promptTask = agent.PromptAsync(input);
                _pendingAgentPrompts[_currentAgentId] = new TaskCompletionSource<string>();
                _ = HandleAgentResponse(agent.Id, promptTask);
            }
            else if (!string.IsNullOrEmpty(_currentAgencyId) && _agencies.TryGetValue(_currentAgencyId, out var agency))
            {
                await agency.InformAsync(input);
            }
        }

        private async Task OnAgentConnected(Agent agent)
        {
            _agents[agent.Id] = agent;
            if (string.IsNullOrEmpty(_currentAgencyId))
            {
                _currentAgencyId = agent.Agency.Id;
                _currentAgentId = agent.Id;
            }
            await DisplayPrompt();
        }

        private async Task OnAgencyConnected(Agency agency)
        {
            _agencies[agency.Id] = agency;
            agency.HistoryUpdated += OnAgencyHistoryUpdated;
            await DisplayPrompt();
        }

        private async Task OnAgencyHistoryUpdated(History history)
        {
            if (_isScrollingEnabled)
            {
                Console.WriteLine($"\n[Agency {history.OwnerId}] History Updated.");
                await DisplayPrompt();
            }
            else
            {
                _logger.LogInformation($"[Agency {history.OwnerId}] History Updated.");
            }
        }

        private async Task HandleAgentResponse(string agentId, Task<string> promptTask)
        {
            string result = await promptTask;
            if (_pendingAgentPrompts.TryRemove(agentId, out var tcs))
            {
                tcs.SetResult(result);
                if (_isScrollingEnabled)
                {
                    Console.WriteLine($"\n{GetNotifications()}\\{agentId}> {result}");
                    await DisplayPrompt();
                }
            }
        }
    }
}