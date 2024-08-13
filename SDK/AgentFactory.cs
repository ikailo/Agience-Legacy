using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Agience.SDK
{
    internal class AgentFactory
    {
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly KernelPluginCollection _hostPlugins;

        internal AgentFactory(Authority authority, Broker broker, KernelPluginCollection hostPlugins)
        {
            _authority = authority;
            _broker = broker;
            _hostPlugins = hostPlugins;
        }

        internal void AddHostPlugin(Models.Entities.Plugin plugin)
        {
            if (plugin.Type == Models.Entities.PluginType.Compiled)
            {
                // TODO: Load with plugin loader. For now, it was added hard coded to the host.
                // Microsoft Time - msTime
                // OpenAI.ChatCompletion
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
            var persona = string.Empty; // TODO: Load persona from agent.

            var agentPlugins = new KernelPluginCollection();

            foreach (var plugin in agent.Plugins)
            {   
                if (plugin.Type == Models.Entities.PluginType.Compiled && _hostPlugins.TryGetPlugin(plugin.Name, out var hostPlugin))
                {
                    agentPlugins.Add(hostPlugin);
                }

                else if (plugin.Type == Models.Entities.PluginType.Curated)
                {
                    var agentFunctions = new List<KernelFunction>();

                    foreach (var function in plugin.Functions)
                    {
                        if (_hostPlugins.TryGetFunction(plugin.Name, function.Name, out var hostFunction))
                        {
                            agentFunctions.Add(hostFunction);
                        }                        
                    }
                    agentPlugins.AddFromFunctions(plugin.Name, agentFunctions);
                }
            }

            var agentServices = new ServiceCollection();

            var apiKey = "";

            

            if (agent.CognitiveFunctionId != null)
            {
                var pluginName = "";
                var functionName = "";

                var cognitiveFunction = _hostPlugins.GetFunction(pluginName, functionName); // This function needs to be already loaded in the host plugins.

                agentServices.AddScoped<IChatCompletionService>(x => new CognitiveFunctionChatCompletionService(cognitiveFunction));
            }

            var kernel = new Kernel(agentServices.BuildServiceProvider(), agentPlugins);

            return new Agent(agent.Id, agent.Name, _authority, _broker, new Models.Entities.Agency() { Id = agent.AgencyId }, persona, kernel);
        }
    }
}