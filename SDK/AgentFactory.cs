using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agience.SDK
{
    internal class AgentFactory
    {
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly ILogger<AgentFactory> _logger;
        private readonly Dictionary<string, Type> _hostPluginsCompiled = new();
        private readonly Dictionary<string, KernelPlugin> _hostPluginsCurated = new();


        internal AgentFactory(Authority authority, Broker broker, ILogger<AgentFactory> logger)
        {
            _authority = authority;
            _broker = broker;
            _logger = logger;
        }

        internal void AddHostPluginFromType<T>(string pluginName) where T : class
        {
            _hostPluginsCompiled.Add(pluginName, typeof(T));
        }

        internal void AddHostPlugin(Models.Entities.Plugin plugin)
        {
            if (string.IsNullOrWhiteSpace(plugin.Name))
            {
                _logger.LogWarning("Plugin name is empty. Plugin will not be loaded.");
                return;
            }

            if (_hostPluginsCompiled.ContainsKey(plugin.Name) || _hostPluginsCurated.ContainsKey(plugin.Name))
            {
                _logger.LogWarning($"{plugin.Name} is already loaded. Plugin will not be loaded.");
                return;
            }

            if (plugin.Type == Models.Entities.PluginType.Compiled)
            {
                var pluginType = Type.GetType(plugin.Name);

                if (pluginType != null)
                {
                    _hostPluginsCompiled.Add(plugin.Name, pluginType);
                }
            }
            else if (plugin.Type == Models.Entities.PluginType.Curated)
            {
                var functions = new List<KernelFunction>();

                foreach (var function in plugin.Functions)
                {
                    functions.Add(KernelFunctionFactory.CreateFromPrompt(function.Prompt!, null as PromptExecutionSettings, function.Name, function.Description, null, null));
                }
                _hostPluginsCurated.Add(plugin.Name, KernelPluginFactory.CreateFromFunctions(plugin.Name, functions));
            }
        }

        internal Agent CreateAgent(Models.Entities.Agent agent, IServiceProvider serviceProvider)
        {
            var persona = string.Empty; // TODO: Load persona from agent.

            var agentPlugins = new KernelPluginCollection();

            foreach (var plugin in agent.Plugins)
            {
                if (string.IsNullOrWhiteSpace(plugin.Name))
                {
                    _logger.LogWarning("Plugin name is empty.");
                    continue;
                }

                if (plugin.Type == Models.Entities.PluginType.Compiled && _hostPluginsCompiled.TryGetValue(plugin.Name, out var pluginType))
                {
                    // TODO: Here we can set parameters to the plugin constructor. Keys, Credentials?
                    agentPlugins.Add(KernelPluginFactory.CreateFromObject(ActivatorUtilities.CreateInstance(serviceProvider, pluginType), plugin.Name));
                }
                else if (plugin.Type == Models.Entities.PluginType.Curated && _hostPluginsCurated.TryGetValue(plugin.Name, out var kernelPlugin))
                {
                    agentPlugins.Add(kernelPlugin);
                }
            }

            if (!string.IsNullOrWhiteSpace(agent.CognitiveFunctionName))
            {
                var (pluginName, functionName) = agent.CognitiveFunctionName.Split('.') switch
                {
                    var parts when parts.Length == 2 => (parts[0], parts[1]),
                    _ => throw new InvalidOperationException($"Invalid CognitiveFunctionName format: {agent.CognitiveFunctionName}")
                };

                if (!agentPlugins.Contains(pluginName))
                {
                    KernelPlugin? kernelPlugin = null;

                    if ( _hostPluginsCompiled.TryGetValue(pluginName, out var pluginType) || _hostPluginsCurated.TryGetValue(pluginName, out kernelPlugin))
                    {
                        kernelPlugin ??= KernelPluginFactory.CreateFromObject(ActivatorUtilities.CreateInstance(serviceProvider, pluginType!), pluginName);

                        if (kernelPlugin.TryGetFunction(functionName, out var kernelFunction))
                        {
                            agentPlugins.AddFromFunctions(pluginName, new[] { kernelFunction });
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Plugin {pluginName} not found. Cognitive function {agent.CognitiveFunctionName} will not be loaded.");
                    }
                }

                var cognitiveFunction = agentPlugins.GetFunction(pluginName, functionName);

                var serviceCollection = new ServiceCollection();
                                
                foreach (var serviceDescriptor in serviceProvider.GetRequiredService<IServiceCollection>())
                {
                    serviceCollection.Add(serviceDescriptor);
                }

                serviceCollection.AddScoped<IChatCompletionService>(sp => new CognitiveFunctionChatCompletionService(cognitiveFunction));
                
                serviceProvider = serviceCollection.BuildServiceProvider(); // Overwriting the serviceProvider to include the new service.
            }

            var kernel = new Kernel(serviceProvider, agentPlugins);

            return new Agent(agent.Id, agent.Name, _authority, _broker, new Models.Entities.Agency() { Id = agent.AgencyId }, persona, kernel);
        }
    }
}