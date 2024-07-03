using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Agience.SDK
{
    internal class AgentFactory
    {
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly IServiceProvider _serviceProvider;
        private readonly KernelPluginCollection _hostPlugins;

        internal AgentFactory(Authority authority, Broker broker, IServiceProvider serviceProvider, KernelPluginCollection hostPlugins)
        {
            _authority = authority;
            _broker = broker;
            _serviceProvider = serviceProvider;
            _hostPlugins = hostPlugins;
        }

        internal Agent CreateAgent(Models.Entities.Agent model)
        {
            var serviceCollection = new ServiceCollection();

            // TODO: Add the services from the model
            /*
            foreach (var serviceName in model.Services)
            {
                var serviceType = Type.GetType(serviceName);
                if (serviceType == null)
                {
                    _logger?.LogWarning($"Service type '{serviceName}' could not be resolved.");
                    continue;
                }

                var serviceInstance = _serviceProvider.GetService(serviceType);
                if (serviceInstance == null)
                {
                    _logger?.LogWarning($"Service instance for type '{serviceName}' could not be retrieved.");
                    continue;
                }

                serviceCollection.AddSingleton(serviceType, serviceInstance);
            }*/

            // For now, just add all the services
            var serviceProvider = _serviceProvider;

            var plugins = new KernelPluginCollection();
            // TODO: Add the functions from the model
            /*
            foreach (var function in model.Functions)
            {
                plugins.Add(_hostPlugins.GetFunction(function.PluginName, function.Name));
            }*/

            // For now, just add all the plugins
            plugins.AddRange(_hostPlugins); 
            

            return new Agent(model.Id, model.Name, _authority, _broker, new Models.Entities.Agency() { Id = model.AgencyId }, null, serviceProvider, plugins);
        }
    }
}
