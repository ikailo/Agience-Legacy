using Agience.Client.MQTT.Model;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Formatter;

namespace Agience.Client.MQTT
{
    public class Broker
    {
        public bool IsConnected => _client.IsConnected;

        private const string MESSAGE_TYPE_KEY = "message.type";

        private readonly IMqttClient _client;
        private readonly string _brokerUri;

        private readonly Dictionary<string, Func<Message, Task>> _callbacks = new();

        public Broker(string brokerUri)
        {
            _brokerUri = brokerUri;
            _client = new MqttFactory().CreateMqttClient();
            _client.ApplicationMessageReceivedAsync += _client_ApplicationMessageReceivedAsync;
        }

        public async Task ConnectAsync(string token)
        {
            if (!_client.IsConnected)
            {
                var options = new MqttClientOptionsBuilder()                    
                    .WithWebSocketServer(configure => { configure.WithUri(_brokerUri); })
                    .WithTlsOptions(configure => { configure.UseTls(true); })
                    .WithCredentials(token, "<no_password>")
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithoutThrowOnNonSuccessfulConnectResponse()
                    .Build();
                await _client.ConnectAsync(options);
            }
        }

        private async Task _client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;

            if (_callbacks.TryGetValue(topic, out var callback))
            {
                var message = new Message()
                {
                    Type = Enum.TryParse<MessageType>(
                        args.ApplicationMessage.UserProperties.FirstOrDefault(x => x.Name == MESSAGE_TYPE_KEY)?.Value.ToString(), out var messageType) ?
                        messageType :
                        MessageType.UNKNOWN,
                    Topic = args.ApplicationMessage.Topic,
                    Payload = args.ApplicationMessage.ConvertPayloadToString()
                };

                if (callback != null)
                {
                    await callback(message);
                }
            }
        }

        public async Task SubscribeAsync(string topic, Func<Message, Task> callback)
        {
            if (!_client.IsConnected) throw new InvalidOperationException("Not Connected");

            _callbacks[topic] = callback; // TODO: Handle multiple callbacks for the same address ?

            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(builder => builder.WithTopic(topic))
                .Build();

            await _client.SubscribeAsync(options);
        }

        public async Task DisconnectAsync()
        {
            await _client.DisconnectAsync();            
        }

        public async Task PublishAsync(Message message)
        {
            if (_client.IsConnected)
            {
                var mqMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(message.Topic ?? throw new ArgumentNullException(nameof(message.Topic)))
                    .WithPayload(message.Payload?.ToString() ?? throw new ArgumentNullException(nameof(message.Payload)))
                    .WithRetainFlag(false)
                    .WithUserProperty(MESSAGE_TYPE_KEY, message.Type.ToString())
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                    .Build();

                await _client.PublishAsync(mqMessage);
            }
        }
    }
}
