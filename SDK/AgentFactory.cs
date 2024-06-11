using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Agience.SDK
{
    internal class AgentFactory
    {
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly ServiceCollection _services;
        private readonly KernelPluginCollection _plugins;

        internal AgentFactory(Authority authority, Broker broker, ServiceCollection services, KernelPluginCollection plugins)
        {
            _authority = authority;
            _broker = broker;
            _services = services;
            _plugins = plugins;
        }

        internal Agent CreateAgent(Models.Agent src)
        {
            return new Agent(src.Id, src.Name, _authority, _broker, src.Agency, null, _services, _plugins);
        }
    }
}
