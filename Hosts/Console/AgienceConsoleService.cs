using Agience.SDK;
using Microsoft.Extensions.Logging;
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

                string input = Console.ReadLine();

                if (input.StartsWith("/"))
                {
                    ProcessCommand(input);
                }
                else
                {
                    ProcessInput(input);
                }
            }
        }

        private void DisplayPrompt()
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

        private void ProcessCommand(string command)
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

        private void ProcessInput(string input)
        {
            if (!string.IsNullOrEmpty(_currentAgentId) && _agents.TryGetValue(_currentAgentId, out var agent))
            {
                var promptTask = agent.PromptAsync(input);
                _pendingAgentPrompts[_currentAgentId] = new TaskCompletionSource<string>();
                _ = HandleAgentResponse(agent.Id, promptTask);
            }
            else if (!string.IsNullOrEmpty(_currentAgencyId) && _agencies.TryGetValue(_currentAgencyId, out var agency))
            {
                agency.Inform(input);
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
            DisplayPrompt();
        }

        private async Task OnAgencyConnected(Agency agency)
        {
            _agencies[agency.Id] = agency;
            agency.HistoryUpdated += OnAgencyHistoryUpdated;
            DisplayPrompt();
        }

        private async Task OnAgencyHistoryUpdated(History history)
        {
            if (_isScrollingEnabled)
            {
                Console.WriteLine($"\n[Agency {history.OwnerId}] History Updated.");
                DisplayPrompt();
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
                    DisplayPrompt();
                }
            }
        }
    }
}