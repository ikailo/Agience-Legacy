using Agience.Agents.Primary.Templates.OpenAI;
using Agience.Agents.Primary.Templates.Process;
using Agience.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Experimental;

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
            _host = new Client.Host(new Config
            {
                AuthorityUri = _appConfig.AuthorityUri,
                ClientId = _appConfig.ClientId,
                ClientSecret = _appConfig.ClientSecret
            });

            KernelPluginCollection plugins = new();
            plugins.AddFromType<Input>();
            plugins.AddFromType<Plan>();
            plugins.AddFromType<Select>();
            plugins.AddFromType<Execute>();
            plugins.AddFromType<Prompt>();            

            ServiceCollection services = new();
            services.AddOpenAIChatCompletion("", "");




            /*
            _host.AddTemplate<Input>();
            _host.AddTemplate<Plan>();
            _host.AddTemplate<Select>();
            _host.AddTemplate<Execute>();
            _host.AddTemplate<Prompt>();
            */

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
            agent.Agency.SetTemplateDefault<Input>("prompt");

            return Task.CompletedTask;

        }

        private Task _host_AgentReady(Agent agent)
        {
            _logger.LogInformation($"{agent.Name} Ready");

            return Task.CompletedTask;
        }
    }
}
