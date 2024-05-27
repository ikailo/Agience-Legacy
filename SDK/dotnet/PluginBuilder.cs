
using Microsoft.Extensions.DependencyInjection;

namespace Agience.Client
{
    public class PluginBuilder
    {
        private string _name;

        public PluginBuilder(string name)
        {
            _name = name;
        }

        public PluginBuilder AddPluginFromType<T>()
        {
            throw new NotImplementedException();
        }

        public PluginBuilder AddService(ServiceDescriptor consoleService)
        {
            throw new NotImplementedException();
        }
    }
}