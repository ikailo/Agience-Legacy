using Agience.SDK.Services;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly string _openAiApiKey;

        private readonly Dictionary<string, Type> _hostPluginsCompiled = new();
        private readonly Dictionary<string, KernelPlugin> _hostPluginsCurated = new();
        private readonly Dictionary<string, Agency> _agencies = new();

        internal AgentFactory(Authority authority, Broker broker, ILogger<AgentFactory> logger, string? openAiApiKey = null)
        {
            _authority = authority;
            _broker = broker;
            _logger = logger;
            _openAiApiKey = openAiApiKey ?? string.Empty;
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

        internal Agent CreateAgent(Models.Entities.Agent modelAgent, IServiceCollection serviceCollection)
        {
            var persona = string.Empty; // TODO: Load persona from agent.

            var agentPlugins = new KernelPluginCollection();

            using var tempServiceProvider = serviceCollection.BuildServiceProvider();

            foreach (var plugin in modelAgent.Plugins)
            {
                if (string.IsNullOrWhiteSpace(plugin.Name))
                {
                    _logger.LogWarning("Plugin name is empty.");
                    continue;
                }

                if (plugin.Type == Models.Entities.PluginType.Compiled && _hostPluginsCompiled.TryGetValue(plugin.Name, out var pluginType))
                {   
                    var kernelPlugin = KernelPluginFactory.CreateFromObject(ActivatorUtilities.CreateInstance(tempServiceProvider, pluginType!), plugin.Name);
                    agentPlugins.Add(kernelPlugin);
                }
                else if (plugin.Type == Models.Entities.PluginType.Curated && _hostPluginsCurated.TryGetValue(plugin.Name, out var kernelPlugin))
                {
                    agentPlugins.Add(kernelPlugin);
                }
            }

            if (!string.IsNullOrWhiteSpace(modelAgent.ChatCompletionFunctionName))
            {
                var (pluginName, functionName) = modelAgent.ChatCompletionFunctionName.Split('.') switch
                {
                    var parts when parts.Length == 2 => (parts[0], parts[1]),
                    _ => throw new InvalidOperationException($"Invalid ChatCompletionFunctionName format: {modelAgent.ChatCompletionFunctionName}")
                };

                if (functionName.EndsWith("Async"))
                {
                    functionName = functionName.Substring(0, functionName.Length - "Async".Length);
                }

                if (!agentPlugins.Contains(pluginName))
                {
                    KernelPlugin? kernelPlugin = null;

                    if (_hostPluginsCompiled.TryGetValue(pluginName, out var pluginType) || _hostPluginsCurated.TryGetValue(pluginName, out kernelPlugin))
                    {
                        kernelPlugin ??= KernelPluginFactory.CreateFromObject(ActivatorUtilities.CreateInstance(tempServiceProvider, pluginType!), pluginName);

                        if (kernelPlugin.TryGetFunction(functionName, out var kernelFunction))
                        {
                            agentPlugins.AddFromFunctions(pluginName, new[] { kernelFunction });
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Plugin {pluginName} not found. Chat Completion function {modelAgent.ChatCompletionFunctionName} will not be loaded.");
                    }
                }

                var chatCompletionFunction = agentPlugins.GetFunction(pluginName, functionName);

                serviceCollection.AddScoped<IChatCompletionService>(sp => new AgienceChatCompletionService(chatCompletionFunction));
            }

            if (!string.IsNullOrWhiteSpace(_openAiApiKey))
            {
                // HERE: Add OpenAI API key to the service collection.
                serviceCollection.AddScoped(sp => new AgienceCredentialService(modelAgent.Id, _authority, _broker));
            }
            

            var kernel = new Kernel(serviceCollection.BuildServiceProvider(), agentPlugins);

            var agency = GetAgency(modelAgent.Agency, kernel.LoggerFactory.CreateLogger<Agency>());

            var agent = new Agent(modelAgent.Id, modelAgent.Name, _authority, _broker, agency, persona, kernel, kernel.LoggerFactory.CreateLogger<Agent>());

            agency.AddLocalAgent(agent);
                        
            return agent;
        }

        internal Agency GetAgency(Models.Entities.Agency modelAgency, ILogger<Agency> logger)
        {
            if (!_agencies.TryGetValue(modelAgency.Id, out var agency))
            {
                agency = new Agency(_authority, _broker, logger)
                {
                    Id = modelAgency.Id,
                    Name = modelAgency.Name
                    // Could use AutoMapper here
                };

                _agencies[modelAgency.Id] = agency;
            }

            return _agencies[modelAgency.Id];
        }
    }
}