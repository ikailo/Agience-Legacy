using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Agience.Client.MQTT.Model
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        private readonly Uri _authorityUri; // Expect without trailing slash
        public string? TokenEndpoint { get; private set; }
        public string? BrokerUri { get; private set; }
        public string Id => _authorityUri.Host;

        public event Func<Data, Task>? InstanceStatusMessage;

        private Broker? _broker;

        public Authority(string authorityUri)
        {
            _authorityUri = new Uri(authorityUri);
        }

        public Authority(string authorityUri, string brokerUri)
        {
            _authorityUri = new Uri(authorityUri);
            BrokerUri = brokerUri;            
        }

        public async Task InitializeAsync()
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_authorityUri.OriginalString}{OPENID_CONFIG_PATH}",
                new OpenIdConnectConfigurationRetriever());

            var configuration = await configurationManager.GetConfigurationAsync();

            BrokerUri = configuration?.AdditionalData[BROKER_URI_KEY].ToString();
            TokenEndpoint = configuration?.TokenEndpoint;
        }

        public async Task Connect(string token)
        {
            if (token == null) { throw new ArgumentNullException(nameof(token)); }

            _broker = new Broker(BrokerUri ?? throw new ArgumentNullException("BrokerUri"));

            await _broker.ConnectAsync(token);

            if (_broker.IsConnected)
            {
                await _broker.SubscribeAsync($"+/{Id}/-/-/-", async message => await _broker_ReceiveMessage(message));
            }
            else
            {
                throw new Exception("Broker is not connected");
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.Type == MessageType.STATUS && message.Payload != null && InstanceStatusMessage != null)
            {
                await InstanceStatusMessage.Invoke(message.Payload);
            }                  
        }
    }
}
