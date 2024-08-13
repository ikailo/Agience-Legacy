using Microsoft.Extensions.Logging;
using Agience.SDK;
using Host = Agience.SDK.Host;

namespace Agience.Hosts._Console
{
    public class AgienceConsoleHost
    {
        private readonly Host _host;
        private readonly ILogger<AgienceConsoleHost> _logger;
        //private Agent? _contextAgent;

        public AgienceConsoleHost(Host host, ILogger<AgienceConsoleHost> logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            //_host.AgentConnected += _host_AgentConnected;
        }

        public async Task Run()
        {
            var hostTask = _host.Run();
            var consoleTask = Task.CompletedTask; // RunConsole();

            await Task.WhenAll(hostTask, consoleTask);
        }
        /*
        private async Task _host_AgentConnected(Agent agent)
        {
            _logger.LogInformation("{AgentName} Ready", agent.Name);

            if (_contextAgent == null)
            {
                _contextAgent = agent;
                _logger.LogInformation("* Switched context to {AgentName} *", agent.Name ?? "Unknown");                
            }
        }

        private async Task RunConsole()
        {
            if (_contextAgent == null) throw new InvalidOperationException("No context agent set");

            Console.Write("User > ");
            string? userInput;

            while ((userInput = Console.ReadLine()) != null)
            {
                var message = await _contextAgent.PromptAsync(userInput);
                Console.WriteLine("{0} > {1}", message.Role, message.ToString());
                Console.Write("User > ");
            }
        }*/
    }
}