using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json;

namespace Agience.Client
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        public event Func<Model.Instance, Task>? InstanceConnected;
        public event Func<Task>? Disconnected;

        public string? TokenEndpoint { get; private set; }
        public string? BrokerUri { get; private set; }
        public string Id => _authorityUri.Host;

        private readonly Uri _authorityUri; // Expect without trailing slash
        private Broker? _broker;
        private bool _isConnected;
        private bool _isSubscribed;

        // TODO: We can probably split this into two classes - like Authority and AuthorityService, for specific use cases.
        
        // This constructor is used by Instance.
        public Authority(string authorityUri)
        {
            _authorityUri = new Uri(authorityUri);
        }

        // This constructor is used in BrokerService when running as an Authority Service.
        public Authority(string authorityUri, string brokerUri)
        {
            _authorityUri = new Uri(authorityUri);
            BrokerUri = brokerUri;

            _broker = new Broker(BrokerUri ?? throw new ArgumentNullException("BrokerUri"));
            _broker.Disconnected += Disconnected;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{_authorityUri.OriginalString}{OPENID_CONFIG_PATH}",
                    new OpenIdConnectConfigurationRetriever());

                var configuration = await configurationManager.GetConfigurationAsync();

                BrokerUri = configuration?.AdditionalData[BROKER_URI_KEY].ToString();
                TokenEndpoint = configuration?.TokenEndpoint;
            }
            catch (Exception ex)
            {
                // TODO: This fails way too often. Need to figure out why.
                throw new Exception($"Failed to initialize authority {_authorityUri.OriginalString}", ex);
            }
        }

        public async Task Connect(string token)
        {
            if (token == null) { throw new ArgumentNullException(nameof(token)); }            

            if (_broker == null) { throw new ArgumentNullException(nameof(_broker)); }

            if (!_isConnected)
            {
                await _broker.ConnectAsync(token);
                await Subscribe();
                _isConnected = true;
            }
        }

        public async Task Subscribe()
        {
            if (_broker == null) { throw new ArgumentNullException(nameof(_broker)); }

            if (!_isSubscribed)
            {
                await _broker.SubscribeAsync($"+/{Id}/-/-/-", async message => await _broker_ReceiveMessage(message));
                _isSubscribed = true;
            }
        }

        public async Task Unsubscribe()
        {
            if (_broker == null) { throw new ArgumentNullException(nameof(_broker)); }

            if (_isSubscribed)
            {
                await _broker.UnsubscribeAsync($"+/{Id}/-/-/-");
                _isSubscribed = false;
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload?.Structured == null) { return; }

            if (message.Type == MessageType.EVENT && message.Payload.Structured?["type"] == "instanceConnect")
            {
                var instance = JsonSerializer.Deserialize<Model.Instance>(message.Payload.Structured["instance"]);

                if (instance?.Id == message.SenderId && InstanceConnected != null)
                {
                    await InstanceConnected.Invoke(instance);
                }
            }
        }

        public async Task PublishAgentConnectEvent(Model.Agent agent)
        {
            if (_broker == null) { throw new ArgumentNullException(nameof(_broker)); }

            if (agent.Instance?.Id == null) { throw new ArgumentNullException(nameof(agent.Instance.Id)); }

            await _broker!.PublishAsync(new Message()
            {
                Type = MessageType.EVENT,
                Topic = $"-/{Id}/{agent.Instance.Id}/-/-",
                Payload = new Data(new()
                {
                    { "type", "agentConnect" },
                    { "agent", JsonSerializer.Serialize(agent) }
                })
            });
        }

        public async Task PublishAgentDisconnectEvent(Model.Agent agent)
        {
            if (_broker == null) { throw new ArgumentNullException(nameof(_broker)); }

            if (agent.Instance?.Id == null) { throw new ArgumentNullException(nameof(agent.Instance.Id)); }

            await _broker!.PublishAsync(new Message()
            {
                Type = MessageType.EVENT,
                Topic = $"-/{Id}/{agent.Instance.Id}/-/-",
                Payload = new Data(new()
                {
                    { "type", "agentDisconnect" },
                    { "agent", JsonSerializer.Serialize(agent) }
                })
            });
        }

        public async Task DisconnectAsync()
        {
            if (_broker != null)
            {
                await Unsubscribe();
                await _broker.DisconnectAsync();
            }
        }
    }
}
