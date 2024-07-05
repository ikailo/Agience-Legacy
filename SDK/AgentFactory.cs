using Microsoft.SemanticKernel;

namespace Agience.SDK
{
    internal class AgentFactory
    {
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly IServiceProvider _hostServices;
        private readonly KernelPluginCollection _hostPlugins;

        internal AgentFactory(Authority authority, Broker broker, IServiceProvider hostServices, KernelPluginCollection hostPlugins)
        {
            _authority = authority;
            _broker = broker;
            _hostServices = hostServices;
            _hostPlugins = hostPlugins;
        }

        internal Agent CreateAgent(Models.Entities.Agent agent)
        {
            // For now, we'll add all the services. Later it will need to be filtered.
            var agentServices = _hostServices;                  

            // Create a Plugin for the Agent that includes all the functions Host Functions defined in the agent's model.
            var functions = new List<KernelFunction>();
            
            foreach (var plugin in agent.Plugins)
            {
                foreach (var function in plugin.Functions)
                {
                    // TODO: Will likely need to deduplicate functions.
                    functions.Add(_hostPlugins.GetFunction(plugin.Name, function.Name));                    
                }                
            }

            var agentPlugins = new KernelPluginCollection();

            agentPlugins.AddFromFunctions(agent.Name, functions);

            return new Agent(agent.Id, agent.Name, _authority, _broker, new Models.Entities.Agency() { Id = agent.AgencyId }, null, agentServices, agentPlugins);
        }
    }
}