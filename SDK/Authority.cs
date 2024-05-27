using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json;

namespace Agience.SDK
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        public event Func<Host, Task>? HostConnected;

        public string Id => _authorityUri.Host;
        public string? BrokerUri { get; private set; }
        public string? TokenEndpoint { get; private set; }
        public bool IsConnected { get; private set; }
        public string Timestamp => _broker.Timestamp;

        private readonly Uri _authorityUri; // Expect without trailing slash
        private readonly Broker _broker = new();


        private static Dictionary<string, string> _defaultTemplates = new()
        {
            { "log", "Agience.Client.Templates.Default.Log" },
            { "context", "Agience.Client.Templates.Default.Context" },
            { "debug", "Agience.Client.Templates.Default.Debug" },
            { "echo", "Agience.Client.Templates.Default.Echo" },
            { "history", "Agience.Client.Templates.Default.History" },
            { "prompt", "Agience.Client.Templates.Default.Prompt" },
        };

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

        private async Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null ||
                message.Data == null //|| 
                                     //message.Payload.Format != DataFormat.STRUCTURED
                ) { return; }

            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "host_connect" &&
                message.Data?["host"] != null)
            {
                var host = JsonSerializer.Deserialize<Host>(message.Data?["host"]!);

                // TODO: Move to seperate method
                if (host?.Id == message.SenderId && HostConnected != null)
                {
                    await HostConnected.Invoke(host);
                }
            }
        }

        public void PublishAgentConnectEvent(Agent agent)
        {
            if (!IsConnected) { throw new InvalidOperationException("Not Connected"); }

            if (agent.Host?.Id == null) { throw new ArgumentNullException(nameof(agent.Host.Id)); }

            _broker.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = HostTopic(Id, agent.Host.Id),
                Data = new Data
                {
                    { "type", "agent_connect" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(agent) },
                    { "default_templates", JsonSerializer.Serialize(_defaultTemplates) }
                }
            });
        }

        public void PublishAgentDisconnectEvent(Agent agent)
        {
            if (!IsConnected) { throw new InvalidOperationException("Not Connected"); }

            if (agent.Host?.Id == null) { throw new ArgumentNullException(nameof(agent.Host.Id)); }

            _broker!.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = HostTopic(Id, agent.Host.Id),
                Data = new Data
                {
                    { "type", "agent_disconnect" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(agent) }
                }
            });
        }

        public string Topic(string senderId, string? hostId, string? agencyId, string? agentId)
        {
            var result = $"{(senderId != Id ? senderId : "-")}/{Id}/{hostId ?? "-"}/{agencyId ?? "-"}/{agentId ?? "-"}";
            return result;
        }

        public string AuthorityTopic(string senderId)
        {
            return Topic(senderId, null, null, null);
        }

        public string HostTopic(string senderId, string? hostId)
        {
            return Topic(senderId, hostId, null, null);
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
