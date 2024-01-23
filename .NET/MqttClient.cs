using Agience.Client.MQTT.Model;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Agience.Client.MQTT
{
    internal class MqttClient
    {
        public bool IsConnected => _client.IsConnected;

        internal event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;

        private IMqttClient _client = new MqttFactory().CreateMqttClient();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private bool _isConnecting;
        //private Identity _identity;

        private const int PORT = 1884;
        /*
        public MqttClient(Identity identity) //, EventHandler<MqttApplicationMessageReceivedEventArgs> _mqtt_MessageReceived)
        {
            _identity = identity;
            //MessageReceived += _mqtt_MessageReceived;
        }*/

        internal async Task ConnectAsync(string brokerUri, string token)
        {
            if (!_client.IsConnected)
            {
                var options = new MqttClientOptionsBuilder()
                .WithWebSocketServer(configure => {
                    configure.Uri = "wss://broker.local.agience.ai:1884"; //brokerUri;
                    configure.TlsOptions = new MqttClientTlsOptions() { UseTls = true, AllowUntrustedCertificates = true };                    
                })
                .WithCredentials(token, "<no_password>")
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build();

                _client.ApplicationMessageReceivedAsync += _client_ApplicationMessageReceivedAsync;

                await _client.ConnectAsync(options, _cancellationTokenSource.Token);
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

        internal async Task PublishAsync(Message message)
        //internal async Task PublishAsync(string topic, string payload, AgentMessageType messageType)
        {
            /*
            if (!_client.IsConnected)
            {
                // TODO: During Debugging, MQTT disconnects after only a few seconds.  Need to figure out why and fix it.
                await ConnectAsync(true);
            }*/

            if (_client.IsConnected)
            {
                var mqMessage = new MqttApplicationMessageBuilder()
                .WithTopic(message.Topic)
                //.WithPayload(message.MessageData)
                .WithRetainFlag(false)
                .WithUserProperty(Message.MESSAGE_TYPE, message.MessageType.ToString())
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

                await _client.PublishAsync(mqMessage, _cancellationTokenSource.Token);

            }
        }
    }
}