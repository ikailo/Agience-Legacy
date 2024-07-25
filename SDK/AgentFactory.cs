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

        internal void AddHostPlugin(Models.Entities.Plugin plugin)
        {
            if (plugin.Type == Models.Entities.PluginType.Compiled)
            {
                // TODO: Load with plugin loader. For now, it was added hard coded to the host.
            }

            else if (plugin.Type == Models.Entities.PluginType.Curated)
            {
                var functions = new List<KernelFunction>();

                foreach (var function in plugin.Functions)
                {
                    functions.Add(KernelFunctionFactory.CreateFromPrompt(function.Prompt!, null as PromptExecutionSettings, function.Name, function.Description, null, null));
                }
                _hostPlugins.AddFromFunctions(plugin.Name!, functions);
            }
        }

        internal Agent CreateAgent(Models.Entities.Agent agent)
        {
            // For now, we'll add all the services. Later it will need to be filtered.
            var agentServices = _hostServices;

            var agentPlugins = new KernelPluginCollection();

            // TODO: Will likely need to deduplicate functions.

            foreach (var plugin in agent.Plugins)
            {
                if (plugin.Type == Models.Entities.PluginType.Compiled && _hostPlugins.TryGetPlugin(plugin.Name, out var hostPlugin))
                {
                    agentPlugins.Add(hostPlugin);
                }

                else if (plugin.Type == Models.Entities.PluginType.Curated)
                {
                    var functions = new List<KernelFunction>();

                    foreach (var function in plugin.Functions)
                    {
                        functions.Add(_hostPlugins.GetFunction(plugin.Name, function.Name));
                    }
                    agentPlugins.AddFromFunctions(plugin.Name, functions);
                }
            }

            return new Agent(agent.Id, agent.Name, _authority, _broker, new Models.Entities.Agency() { Id = agent.AgencyId }, null, agentServices, agentPlugins);
        }
    }
}