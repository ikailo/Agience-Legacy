using Agience.SDK;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Agience.Hosts._Console
{
    public class AgienceConsoleService
    {
        private readonly Host _host;
        private readonly ILogger<AgienceConsoleService> _logger;

        private string? _contextAgentId;
        private string? _contextAgencyId;
        private bool _scrollingMode = true;

        private readonly Dictionary<string, Agent> _agents = new();
        private readonly Dictionary<string, Agency> _agencies = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingAgentPrompts = new();

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
                else if (!string.IsNullOrEmpty(_contextAgentId))
                {
                    if (_agents.TryGetValue(_contextAgentId, out var agent))
                    {
                        var promptTask = agent.PromptAsync(input);
                        _pendingAgentPrompts[_contextAgentId] = new TaskCompletionSource<string>();

                        _ = HandleAgentResponse(agent.Id, promptTask);
                    }
                }
                else if (!string.IsNullOrEmpty(_contextAgencyId))
                {
                    if (_agencies.TryGetValue(_contextAgencyId, out var agency))
                    {
                        agency.Inform(input);
                    }
                }
            }
        }

        private void DisplayPrompt()
        {
            string notifications = GetNotifications();

            if (!string.IsNullOrEmpty(_contextAgentId))
            {
                Console.Write($"{notifications} \\{_contextAgencyId}\\{_contextAgentId}> ");
            }
            else if (!string.IsNullOrEmpty(_contextAgencyId))
            {
                Console.Write($"{notifications} \\{_contextAgencyId}> ");
            }
            else
            {
                Console.Write("> ");
            }
        }

        private string GetNotifications()
        {
            // This is a placeholder for actual notification logic.
            // It should return a string based on the current notification state.
            return "[notif]";
        }

        private void ProcessCommand(string command)
        {
            switch (command)
            {
                case "/scroll":
                    _scrollingMode = true;
                    break;
                case "/notify":
                    _scrollingMode = false;
                    break;
                //case "/debug on":
                //    _logger.LogDebug("Debug logging enabled.");
                //    break;
                //case "/debug off":
                //    _logger.LogDebug("Debug logging disabled.");
                //    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private async Task OnAgentConnected(Agent agent)
        {
            _agents[agent.Id] = agent;

            // TEMP: Set the context to the first agent connected.
            if (string.IsNullOrEmpty(_contextAgencyId))
            {
                _contextAgencyId = agent.Agency.Id;
            }

            if (string.IsNullOrEmpty(_contextAgentId))
            {                
                _contextAgentId = agent.Id;
            }
        }

        private async Task OnAgencyConnected(Agency agency)
        {
            _agencies[agency.Id] = agency;
            agency.HistoryUpdated += OnAgencyHistoryUpdated;
        }


        private async Task OnAgencyHistoryUpdated(History history)
        {
            if (_scrollingMode)
            {
                Console.WriteLine($"\n[Agency {history.OwnerId}] History Updated.");
                DisplayPrompt();
            }
            else
            {
                _logger.LogInformation($"[Agency {history.OwnerId}] history updated.");
                // Implement buffered notification logic here.
            }
        }

        private async Task HandleAgentResponse(string agentId, Task<string> promptTask)
        {
            string result = await promptTask;

            if (_pendingAgentPrompts.TryRemove(agentId, out var tcs))
            {
                tcs.SetResult(result);

                if (!_scrollingMode)
                {
                    // Notify the user of the agent response
                    Console.WriteLine($"\n[Notification] Agent {agentId} responded: {result}");
                }

                DisplayPrompt();
            }
        }
    }
}