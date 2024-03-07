using Agience.Agents.Primary.Plugins;
using Agience.Client;
using HostBuilder = Agience.Client.HostBuilder;

namespace Agience.Agents.Primary
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppConfig _appConfig;

        private Client.Host? _host;

        public Worker(ILogger<Worker> logger, AppConfig appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_appConfig.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
            if (string.IsNullOrEmpty(_appConfig.ClientId)) { throw new ArgumentNullException("ClientId"); }
            if (string.IsNullOrEmpty(_appConfig.ClientSecret)) { throw new ArgumentNullException("ClientSecret"); }

            var builder = new HostBuilder()
                .WithAuthorityUri(_appConfig.AuthorityUri)
                .WithCredentials(_appConfig.ClientId, _appConfig.ClientSecret)
                .AddPluginFromType<ProcessPlugin>();

            _host = builder.Build();

            //_host.AddService();
            //_host.AddAgentBuilder

            _host.AgentReady += _host_AgentReady;
            _host.AgentConnected += _host_AgentConnected;

            _logger.LogInformation("Starting Host");

            await _host.Run();

            _logger.LogInformation("Host Stopped");
        }

        private Task _host_AgentConnected(Agent agent)
        {
            _logger.LogInformation($"{agent.Agency.Name} / {agent.Name} Connected");

            // Register template defaults
            //agent.Agency.SetTemplateDefault<Input>("prompt");

            return Task.CompletedTask;

        }

        private Task _host_AgentReady(Agent agent)
        {
            _logger.LogInformation($"{agent.Name} Ready");

            return Task.CompletedTask;
        }
    }
}
