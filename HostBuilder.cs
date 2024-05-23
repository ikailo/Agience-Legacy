using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;

namespace Agience.SDK
{
    public class HostBuilder
    {
        private readonly KernelPluginCollection _plugins = new(); 
        private readonly ServiceCollection _services = new();
        private readonly Dictionary<string, OutputCallback> _callbacks = new();

        private string? _name;
        private string? _authorityUri;
        private string? _clientId;
        private string? _clientSecret;
        private string? _brokerUriOverride;

        public HostBuilder() { }

        public HostBuilder(string? name)
        {
            _name = name;
        }

        public Host Build()
        {
            if (string.IsNullOrEmpty(_name))
            {
                throw new ArgumentNullException("_name");
            }
            if (string.IsNullOrEmpty(_authorityUri))
            {
                throw new ArgumentNullException("_authorityUri");
            }
            if (string.IsNullOrEmpty(_clientId))
            {
                throw new ArgumentNullException("_clientId");
            }
            if (string.IsNullOrEmpty(_clientSecret))
            {
                throw new ArgumentNullException("_clientSecret");
            }

            Host host = new(_name, _authorityUri, _clientId, _clientSecret, _brokerUriOverride);

            host.AddServices(_services);
            host.AddPlugins(_plugins);

            return host;
        }

        public HostBuilder WithName(string name)
        {
            this._name = name;

            return this;
        }

        public HostBuilder WithAuthorityUri(string authorityUri)
        {
            this._authorityUri = authorityUri;

            return this;
        }

        public HostBuilder WithCredentials(string clientId, string clientSecret)
        {
            this._clientId = clientId;
            this._clientSecret = clientSecret;

            return this;
        }

        public HostBuilder WithBrokerUriOverride(string? brokerUriOverride)
        {
            this._brokerUriOverride = brokerUriOverride;

            return this;
        }

        public HostBuilder AddPluginFromType<T>(string? pluginName = null, IServiceProvider? serviceProvider = null)
        {
            this._plugins.AddFromType<T>(pluginName, serviceProvider);

            return this;
        }


        public HostBuilder AddPlugin(KernelPlugin? plugin)
        {
            if (plugin != null)
            {
                this._plugins.Add(plugin);
            }

            return this;
        }

        public HostBuilder AddPlugins(IEnumerable<KernelPlugin> plugins)
        {
            this._plugins.AddRange(plugins);

            return this;
        }


        public HostBuilder AddService(ServiceDescriptor service)
        {
            if (service != null)
            {
                _services.Add(service);
            }

            return this;
        }

        public HostBuilder WithServices(IEnumerable<ServiceDescriptor> services)
        {
            foreach (var service in services)
            {
                _services.Add(service);
            }

            return this;
        }
        /*
        public HostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            configureServices(_services);
            return this; // Return the builder to allow for method chaining
        }*/
    }
}