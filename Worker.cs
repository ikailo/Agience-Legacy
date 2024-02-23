using OpenAI = Agience.Agents.Primary.Templates.OpenAI;
using Agience.Client;

namespace Agience.Agents.Primary
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppConfig _appConfig;

        private Instance? _instance;

        public Worker(ILogger<Worker> logger, AppConfig appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _instance = new Instance(new Config
            {
                AuthorityUri = _appConfig.AuthorityUri,
                ClientId = _appConfig.ClientId,
                ClientSecret = _appConfig.ClientSecret                
                
            });

            _instance.AddTemplate<OpenAI.Prompt>();

            _instance.AgentReady += _instance_AgentReady;
            _instance.AgentConnected += _instance_AgentConnected;

            _logger.LogInformation("Starting Instance");

            await _instance.Run();

            _logger.LogInformation("Instance Stopped");
        }

        private Task _instance_AgentConnected(Agent agent)
        {
            _logger.LogInformation($"{agent.Agency.Name} / {agent.Name} Connected");

            // Register template defaults
            agent.Agency.SetTemplateDefault<OpenAI.Prompt>("prompt");

            return Task.CompletedTask;

        }

        private Task _instance_AgentReady(Agent agent)
        {
            _logger.LogInformation($"{agent.Name} Ready");

            return Task.CompletedTask;
        }
    }
}
