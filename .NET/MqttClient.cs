using IdentityModel;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Technologai
{
    internal class MqttClient
    {

        private const int PORT = 8884;

        private Identity _identity;

        private IMqttClient _client = new MqttFactory().CreateMqttClient();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public bool IsConnected => _client.IsConnected;

        private bool _isConnecting;

        internal event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;

        public MqttClient(Identity identity, EventHandler<MqttApplicationMessageReceivedEventArgs> _mqtt_MessageReceived)
        {
            _identity = identity;
            MessageReceived += _mqtt_MessageReceived;
        }

        internal async Task ConnectAsync(bool doDisconnect = false)
        {
            if (!_client.IsConnected && !_isConnecting)
            {
                _isConnecting = true;
                var options = new MqttClientOptionsBuilder()
                //.WithWebSocketServer($"broker.staging.technologai.com:{PORT}")
                //.WithTcpServer($"broker-qvn0i6zv.b4a.run", 8883)
                .WithWebSocketServer($"localhost:{PORT}")
                //.WithWebSocketServer($"{new Uri(_identity.Authority.BrokerUri).Host}:{PORT}")
                //.WithTls()
                .WithCredentials(_identity.Tokens[_identity.Authority.BrokerUri], "password")
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build();

                _client.ApplicationMessageReceivedAsync += _client_ApplicationMessageReceivedAsync;

                await _client.ConnectAsync(options, _cancellationTokenSource.Token);
                _isConnecting = false;
            }
        }

        private Task _client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            MessageReceived?.Invoke(this, arg);
            return Task.CompletedTask;
        }

        internal async Task SubscribeAsync(string subscribeMask)
        {
            if (!_client.IsConnected) { throw new InvalidOperationException("Not Connected"); }

            var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(subscribeMask)            
            .Build();

            await _client.SubscribeAsync(options, _cancellationTokenSource.Token);
        }

        internal async Task DisconnectAsync()
        {
            _cancellationTokenSource.Cancel();
            await _client.DisconnectAsync();
            _client.Dispose();
        }

        internal async Task PublishAsync(string topic, string payload, AgentMessageType messageType)
        {
            if (!_client.IsConnected)
            {
                // TODO: During Debugging, MQTT disconnects after only a few seconds.  Need to figure out why and fix it.
                await ConnectAsync(true);
            }

            if (_client.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(false)
                .WithUserProperty(BrokerMessage.MESSAGE_TYPE, messageType.ToString())
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

                await _client.PublishAsync(message, _cancellationTokenSource.Token);

            }
        }
    }
}