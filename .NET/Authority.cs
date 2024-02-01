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
            if (authorityUri == null) { throw new ArgumentNullException(nameof(authorityUri)); }

            _authorityUri = new Uri(authorityUri);
        }

        // This constructor is used when running as an Authority Service.
        public Authority(string authorityUri, string brokerUri)
            : this(authorityUri)
        {
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
                await _broker.SubscribeAsync(AuthorityTopic("+"), async message => await _broker_ReceiveMessage(message));
                _isSubscribed = true;
            }
        }

        public async Task Unsubscribe()
        {
            if (_broker == null) { throw new ArgumentNullException(nameof(_broker)); }

            if (_isSubscribed)
            {
                await _broker.UnsubscribeAsync(AuthorityTopic("+"));
                _isSubscribed = false;
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload == null || message.Payload.Format != DataFormat.STRUCTURED) { return; }

            if (message.Type == MessageType.EVENT && message.Payload["type"] == "instanceConnect" && message.Payload.ContainsKey("instance"))
            {
                var instance = JsonSerializer.Deserialize<Model.Instance>(message.Payload["instance"]!);

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
                Topic = InstanceTopic(Id, agent.Instance.Id),
                Payload = new Data(new()
                {
                    { "type", "agentConnect" },
                    { "timestamp", _broker.Timestamp},
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
                Topic = InstanceTopic(Id, agent.Instance.Id),
                Payload = new Data(new()
                {
                    { "type", "agentDisconnect" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(agent) }
                })
            });
        }

        public string Topic(string senderId, string? instanceId, string? agencyId, string? agentId)
        {
            var result = $"{(senderId != Id ? senderId : "-")}/{Id}/{instanceId ?? "-"}/{agencyId ?? "-"}/{agentId ?? "-"}";
            return result;
        }

        public string AuthorityTopic(string senderId)
        {
            return Topic(senderId, null, null, null);
        }

        public string InstanceTopic(string senderId, string? instanceId)
        {
            return Topic(senderId, instanceId, null, null);
        }

        public string AgencyTopic(string senderId, string agencyId)
        {
            return Topic(senderId, null, agencyId, null);
        }

        public string AgentTopic(string senderId, string agentId)
        {
            return Topic(senderId, null, null, agentId);
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
