using Agience.SDK.Services;
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
        private readonly string _hostOpenAiApiKey;

        private readonly Dictionary<string, Type> _hostPluginsCompiled = new();
        private readonly Dictionary<string, KernelPlugin> _hostPluginsCurated = new();
        private readonly Dictionary<string, Agency> _agencies = new();

        internal AgentFactory(Authority authority, Broker broker, ILogger<AgentFactory> logger, string? hostOpenAiApiKey = null)
        {
            _authority = authority;
            _broker = broker;
            _logger = logger;
            _hostOpenAiApiKey = hostOpenAiApiKey ?? string.Empty;
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

        internal Agent CreateAgent(Models.Entities.Agent modelAgent, IServiceCollection hostServiceCollection)
        {
            var persona = modelAgent.Persona ?? string.Empty;

            var agentPlugins = new KernelPluginCollection();

            var kernelServiceCollection = new ServiceCollection();
                        
            foreach (var serviceDescriptor in hostServiceCollection)
            {
                kernelServiceCollection.Add(serviceDescriptor);
            }

            using var tempServiceProvider = kernelServiceCollection.BuildServiceProvider();

            var credentialService = new AgienceCredentialService(modelAgent.Id, _authority, _broker);

            if (!string.IsNullOrWhiteSpace(_hostOpenAiApiKey))
            {
                credentialService.AddCredential("HostOpenAiApiKey", _hostOpenAiApiKey);
            }

            foreach (var plugin in modelAgent.Plugins)
            {
                if (string.IsNullOrWhiteSpace(plugin.Name))
                {
                    _logger.LogWarning("Plugin name is empty.");
                    continue;
                }

                if (plugin.Type == Models.Entities.PluginType.Compiled && _hostPluginsCompiled.TryGetValue(plugin.Name, out var pluginType))
                {
                    var kernelPlugin = CreateKernelPlugin(tempServiceProvider, pluginType, plugin.Name, credentialService);

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
                        kernelPlugin = CreateKernelPlugin(tempServiceProvider, pluginType!, pluginName, credentialService);

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

                kernelServiceCollection.AddScoped<IChatCompletionService>(sp => new AgienceChatCompletionService(chatCompletionFunction));
            }

            var kernel = new Kernel(kernelServiceCollection.BuildServiceProvider(), agentPlugins);
            var agency = GetAgency(modelAgent.Agency, kernel.LoggerFactory.CreateLogger<Agency>());
            var agent = new Agent(modelAgent.Id, modelAgent.Name, _authority, _broker, agency, persona, kernel, kernel.LoggerFactory.CreateLogger<Agent>());

            agency.AddLocalAgent(agent);

            return agent;
        }

        private KernelPlugin CreateKernelPlugin(IServiceProvider serviceProvider, Type pluginType, string pluginName, AgienceCredentialService credentialService)
        {

            if (pluginType.GetConstructor([typeof(AgienceCredentialService)]) != null)
            {
                return KernelPluginFactory.CreateFromObject(ActivatorUtilities.CreateInstance(serviceProvider, pluginType, credentialService), pluginName);
            }
            else
            {
                return KernelPluginFactory.CreateFromObject(ActivatorUtilities.CreateInstance(serviceProvider, pluginType), pluginName);
            }

            /*
            var connectionAttribute = pluginType.GetCustomAttribute<PluginConnectionAttribute>();
            
            if (connectionAttribute != null)
            {
                // Could potentially do some binding or injection here
            }
            */
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