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


        public string Id => _authorityUri.Host;
        public string? BrokerUri { get; private set; }
        public string? TokenEndpoint { get; private set; }
        public bool IsConnected { get; private set; }

        private readonly Uri _authorityUri; // Expect without trailing slash
        private readonly Broker _broker = new();
        private bool _isSubscribed;

        public Authority(string authorityUri)
        {
            if (authorityUri == null) { throw new ArgumentNullException(nameof(authorityUri)); }

            _authorityUri = new Uri(authorityUri);
        }

        internal async Task Initialize()
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

        public async Task Connect(string accessToken, string brokerUri)
        {
            if (BrokerUri == null)
            {
                await Initialize();
            }

            if (!IsConnected)
            {
                await _broker.Connect(accessToken, brokerUri);
                await _broker.Subscribe(AuthorityTopic("+"), async message => await _broker_ReceiveMessage(message));
                IsConnected = true;
            }
        }

        public async Task Disconnect()
        {
            if (IsConnected)
            {
                await _broker.Unsubscribe(AuthorityTopic("+"));
                await _broker.Disconnect();
                IsConnected = false;
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload == null || message.Payload.Format != DataFormat.STRUCTURED) { return; }

            // TODO: Move to seperate method
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
            if (!IsConnected) { throw new InvalidOperationException("Not Connected"); }

            if (agent.Instance?.Id == null) { throw new ArgumentNullException(nameof(agent.Instance.Id)); }

            await _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = InstanceTopic(Id, agent.Instance.Id),
                Payload = new Data(new()
                {
                    { "type", "agentConnect" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(agent) }
                    // TODO: Add default templates here.
                })
            });
        }

        public async Task PublishAgentDisconnectEvent(Model.Agent agent)
        {
            if (!IsConnected) { throw new InvalidOperationException("Not Connected"); }

            if (agent.Instance?.Id == null) { throw new ArgumentNullException(nameof(agent.Instance.Id)); }

            await _broker!.Publish(new Message()
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
    }
}
