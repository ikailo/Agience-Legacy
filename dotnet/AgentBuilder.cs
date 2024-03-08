using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;

namespace Agience.Client
{
    public class AgentBuilder
    {
        private readonly KernelPluginCollection _plugins = new();

        private Func<HttpClient>? _httpClientProvider;

        private string? _description;
        private string? _name;
        private string? _persona;

        private Authority? _authority;
        private Broker? _broker;
        private Model.Agency? _agency;

        public AgentBuilder() { }

        public AgentBuilder(string? name)
        {
            _name = name;
        }

        public Agent Build()
        {
            if (_authority == null)
            {
                throw new ArgumentNullException("_authority");
            }
            if (_broker == null)
            {
                throw new ArgumentNullException("_broker");
            }
            if (_agency == null)
            {
                throw new ArgumentNullException("_agency");
            }

            // _httpClientProvider ??= () => new HttpClient();

            return new Agent(_authority, _broker, _agency, _persona);
        }

        /*    
        public AgentBuilder FromTemplate(string template)
        {
            this._config = KernelFunctionYaml.ToPromptTemplateConfig(template);

            this.WithInstructions(this._config.Template.Trim());

            if (!string.IsNullOrWhiteSpace(this._config.Name))
            {
                this.WithName(this._config.Name?.Trim());
            }

            if (!string.IsNullOrWhiteSpace(this._config.Description))
            {
                this.WithDescription(this._config.Description?.Trim());
            }

            return this;
        }

        public AgentBuilder FromTemplatePath(string templatePath)
        {
            var yamlContent = File.ReadAllText(templatePath);

            return this.FromTemplate(yamlContent);
        }
        */

        public AgentBuilder WithHttpClient(HttpClient httpClient)
        {
            _httpClientProvider ??= () => httpClient;

            return this;
        }

        public AgentBuilder WithDescription(string? description)
        {
            _description = description;

            return this;
        }

        /*
        public AgentBuilder WithMetadata(string key, object value)
        {
            this._model.Metadata[key] = value;

            return this;
        }    

        public AgentBuilder WithMetadata(IDictionary<string, object> metadata)
        {
            foreach (var kvp in metadata)
            {
                this._model.Metadata[kvp.Key] = kvp.Value;
            }
            return this;
        }
        */

        public AgentBuilder WithName(string? name)
        {
            _name = name;

            return this;
        }


        public AgentBuilder WithPlugin(KernelPlugin? plugin)
        {
            if (plugin != null)
            {
                _plugins.Add(plugin);
            }

            return this;
        }

        public AgentBuilder WithPlugins(IEnumerable<KernelPlugin> plugins)
        {
            this._plugins.AddRange(plugins);

            return this;
        }

        public AgentBuilder AddFunctionCallbackForType<T>(string functionName, OutputCallback callback)
        {
            //_plugins.AddFunctionCallback<T>(functionName, callback);


            throw new NotImplementedException();

            return this;
        }

        public AgentBuilder WithPersona(string persona)
        {
            this._persona = persona;

            return this;
        }

        /*
        public AgentBuilder WithService(ServiceDescriptor service)
        {
            if (service != null)
            {
                _services.Add(service);
            }

            return this;
        }

        public AgentBuilder WithServices(IEnumerable<ServiceDescriptor> services)
        {
            foreach (var service in services)
            {
                _services.Add(service);
            }

            return this;
        }*/
    }
}