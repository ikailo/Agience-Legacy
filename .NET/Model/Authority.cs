using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json;

namespace Agience.Client.Model
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        private readonly Uri _authorityUri; // Expect without trailing slash
        public string? TokenEndpoint { get; private set; }
        public string? BrokerUri { get; private set; }
        public string Id => _authorityUri.Host;

        public delegate Task<List<Agience.Model.Agent>> InstanceConnectedArgs(Agience.Model.Instance instance);

        public event InstanceConnectedArgs? InstanceConnected;

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
            if (message.SenderId == null || message.Payload?.Structured == null) { return; }

            if (InstanceConnected != null && message.Type == MessageType.EVENT && message.Payload.Structured?["type"] == "instanceConnect")
            {
                var instance = JsonSerializer.Deserialize<Agience.Model.Instance>(message.Payload.Structured["instance"]);

                if (instance?.Id == message.SenderId)
                {
                    foreach (var agent in await InstanceConnected.Invoke(instance))
                    {
                        await _broker!.PublishAsync(new Message()
                        {
                            Type = MessageType.EVENT,
                            Topic = $"-/{Id}/{instance.Id}/-/-",
                            Payload = new Data(new()
                            {
                                { "type", "agentConnect" },
                                { "agent", JsonSerializer.Serialize(agent) }
                            })
                        });

                    }
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (_broker != null)
            {
                await _broker.DisconnectAsync();
            }
        }
    }
}
