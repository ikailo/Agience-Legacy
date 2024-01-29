using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Formatter;

namespace Agience.Client
{
    public class Broker
    {
        public bool IsConnected => _client.IsConnected;

        private const string MESSAGE_TYPE_KEY = "message.type";

        private readonly IMqttClient _client;
        private readonly string _brokerUri;

        private readonly Dictionary<string, List<CallbackContainer>> _callbacks = new();

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
                    .WithCleanSession()
                    .Build();
                await _client.ConnectAsync(options);
            }
        }

        private async Task _client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;
            var callbackTopic = topic.Substring(topic.IndexOf('/') + 1); // Remove the SenderId segment

            if (_callbacks.TryGetValue(callbackTopic, out var callbackContainers))
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

                var callbackTasks = new List<Task>();
                foreach (var container in callbackContainers)
                {
                    if (container.Callback != null)
                    {
                        callbackTasks.Add(container.Callback(message));
                    }
                }

                await Task.WhenAll(callbackTasks).ConfigureAwait(false);
            }
        }

        public async Task<Guid> SubscribeAsync(string topic, Func<Message, Task> callback)
        {
            if (!_client.IsConnected) throw new InvalidOperationException("Not Connected");

            var callbackTopic = topic.Substring(topic.IndexOf('/') + 1); // Remove the SenderId segment

            var container = new CallbackContainer(callback);
            if (!_callbacks.ContainsKey(callbackTopic))
            {
                _callbacks[callbackTopic] = new List<CallbackContainer>();
            }
            _callbacks[callbackTopic].Add(container);

            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(builder => builder.WithTopic(topic))
                .Build();

            await _client.SubscribeAsync(options);

            return container.Id;
        }

        public async Task DisconnectAsync()
        {
            _callbacks.Clear(); 
            await _client.DisconnectAsync();
        }

        public async Task Unsubscribe(string topic, Guid callbackId)
        {
            var callbackTopic = topic.Substring(topic.IndexOf('/') + 1);

            if (_callbacks.ContainsKey(callbackTopic))
            {
                var container = _callbacks[callbackTopic].FirstOrDefault(c => c.Id == callbackId);
                if (container != null)
                {
                    _callbacks[callbackTopic].Remove(container);
                    if (!_callbacks[callbackTopic].Any())
                    {
                        _callbacks.Remove(callbackTopic);
                        await _client.UnsubscribeAsync(callbackTopic);
                    }
                }
            }
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

    internal class CallbackContainer
    {
        public Guid Id { get; } = Guid.NewGuid(); // Unique identifier for the callback
        public Func<Message, Task> Callback { get; set; }

        public CallbackContainer(Func<Message, Task> callback)
        {
            Callback = callback;
        }
    }
}
