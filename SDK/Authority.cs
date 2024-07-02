using AutoMapper;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json;
using Agience.SDK.Mappings;
using Microsoft.Extensions.Logging;

namespace Agience.SDK
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        public event Func<Models.Host, Task>? HostConnected;

        public string Id => _authorityUri.Host;
        public string? BrokerUri { get; private set; }
        public string? TokenEndpoint { get; private set; }
        public bool IsConnected { get; private set; }
        public string Timestamp => _broker.Timestamp;

        private readonly Uri _authorityUri; // Expect without trailing slash
        private readonly Broker _broker;
        private readonly ILogger<Authority> _logger;
        private readonly IMapper _mapper;

        public Authority() { }

        public Authority(string authorityUri, Broker broker, ILogger<Authority>? logger = null)
        {
            _authorityUri = !string.IsNullOrEmpty(authorityUri) ? new Uri(authorityUri) : throw new ArgumentNullException(nameof(authorityUri));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = AutoMapperConfig.GetMapper();
        }

        internal async Task InitializeWithBackoff(double maxDelaySeconds = 16)
        {
            if (!string.IsNullOrEmpty(BrokerUri) && !string.IsNullOrEmpty(TokenEndpoint))
            {
                _logger.LogInformation("Authority already initialized.");
                return;
            }

            var delay = TimeSpan.FromSeconds(1);

            while (true)
            {
                try
                {
                    _logger.LogInformation($"Initializing Authority: {_authorityUri.OriginalString}");

                    var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{_authorityUri.OriginalString}{OPENID_CONFIG_PATH}",
                    new OpenIdConnectConfigurationRetriever());

                    var configuration = await configurationManager.GetConfigurationAsync();

                    BrokerUri = configuration?.AdditionalData[BROKER_URI_KEY].ToString();
                    TokenEndpoint = configuration?.TokenEndpoint;

                    _logger.LogInformation($"Authority initialized.");

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex.ToString());
                    _logger.LogInformation($"Unable to initialize Authority. Retrying in {delay.TotalSeconds} seconds.");

                    await Task.Delay(delay);

                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelaySeconds));
                }
            }
        }

        public async Task Connect(string accessToken)
        {
            if (!IsConnected)
            {
                if (string.IsNullOrEmpty(BrokerUri))
                {
                    await InitializeWithBackoff();
                }

                var brokerUri = BrokerUri ?? throw new ArgumentNullException("BrokerUri");

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
                var host = JsonSerializer.Deserialize<Models.Host>(message.Data?["host"]!);

                // TODO: Move to seperate method
                if (host?.Id == message.SenderId && HostConnected != null)
                {
                    await HostConnected.Invoke(host);
                }
            }
        }

        public void PublishAgentConnectEvent(Models.Agent agent)
        {
            throw new NotImplementedException();
            /*
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
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Agent>(agent)) }                    
                }
            });*/
        }

        public void PublishAgentDisconnectEvent(Models.Agent agent)
        {
            throw new NotImplementedException();
            /*
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
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Agent>(agent)) }
                }
            }); */
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
