using Agience.SDK;

namespace Agience.Hosts.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private SDK.Host _host;        

        public Worker(SDK.Host host, ILogger<Worker> logger)
        {
            _logger = logger;
            _host = host;
<<<<<<< Updated upstream
            
            _host.AgentConnected += _host_AgentConnected;
=======

            //_host.AgentBuilding += _host_AgentBuilding;
            _host.AgentConnected += _host_AgentConnected;
            _host.AgentReady += _host_AgentReady;
>>>>>>> Stashed changes
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Host");

            await _host.Run();

            _logger.LogInformation("Host Stopped");
        }


        private Task _host_AgentConnected(Agent agent)
        {
<<<<<<< Updated upstream
=======
            _logger.LogInformation($"{agent.Agency.Name} / {agent.Name} Connected");

            return Task.CompletedTask;

        }

        private Task _host_AgentReady(Agent agent)
        {
>>>>>>> Stashed changes
            _logger.LogInformation($"{agent.Name} Ready");

            return Task.CompletedTask;
        }
    }
}
