using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Formatter;
using MQTTnet.Diagnostics;
using GuerrillaNtp;
using Timer = System.Timers.Timer;

namespace Agience.Client
{
    public class Broker
    {
        internal event Func<Task> Disconnected
        {
            add => _client.DisconnectedAsync += async (args) => await value();
            remove => _client.DisconnectedAsync -= async (args) => await value();
        }

        public string Timestamp => (_ntpClient.Last ?? throw new InvalidOperationException()).Now.UtcDateTime.ToString(TIME_FORMAT);

        private NtpClient _ntpClient = NtpClient.Default; // TODO: Allow custom NTP server
        private Timer _ntpTimer = new Timer(TimeSpan.FromDays(1).TotalMilliseconds); // Synchronize daily

        public bool IsConnected => _client.IsConnected;

        private const string MESSAGE_TYPE_KEY = "message.type";
        private const string TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fff";

        private readonly IMqttClient _client;
        private readonly Dictionary<string, List<CallbackContainer>> _callbacks = new();

        internal Broker()
        {
            _client = new MqttFactory().CreateMqttClient(new MqttNetLogger() { IsEnabled = true });
            _client.ApplicationMessageReceivedAsync += _client_ApplicationMessageReceivedAsync;
        }

        internal async Task Connect(string token, string brokerUri)
        {
            await StartNtpClock();

            // TODO: Write to local logger
            //Console.WriteLine($"Connected Status: {IsConnected}");

            if (!_client.IsConnected)
            {
                Console.WriteLine($"Connecting to {brokerUri}");

                var options = new MqttClientOptionsBuilder()
                    .WithWebSocketServer(configure => { configure.WithUri(brokerUri); })
                    .WithTlsOptions(configure => { configure.UseTls(true); })
                    .WithCredentials(token, "<no_password>")
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithoutThrowOnNonSuccessfulConnectResponse()
                    .WithTimeout(TimeSpan.FromSeconds(300))
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .WithSessionExpiryInterval(60)
                    .WithCleanStart()
                    .Build();

                await _client.ConnectAsync(options);
                               
                Console.WriteLine($"Broker Connected");
            }
        }

        private Task _client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
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

                foreach (var container in callbackContainers)
                {
                    if (container.Callback != null)
                    {
                        Task.Run(() => container.Callback(message))
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                // Rethrow the exception on the ThreadPool
                                ThreadPool.QueueUserWorkItem(_ => { throw t.Exception; });
                            }
                        });
                    }
                }
            }
            return Task.CompletedTask;
        }

        internal async Task Subscribe(string topic, Func<Message, Task> callback)
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
                .WithTopicFilter(topic, MqttQualityOfServiceLevel.AtMostOnce)                
                .Build();

            await _client.SubscribeAsync(options);
        }

        internal async Task Disconnect()
        {
            if (_client.IsConnected)
            {
                await _client.TryDisconnectAsync();
            }
        }

        internal async Task Unsubscribe(string topic)
        {
            var callbackTopic = topic.Substring(topic.IndexOf('/') + 1);

            _callbacks.Remove(callbackTopic);

            await _client.UnsubscribeAsync(callbackTopic);
        }

        internal async Task Publish(Message message)
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

        private class MqttNetLogger : IMqttNetLogger
        {
            public bool IsEnabled { get; internal set; }

            public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
            {
                // TODO: Write to real logger
                // Console.WriteLine($"{logLevel}: {source} - {message}");
            }
        }

        private async Task StartNtpClock()
        {
            await QueryNtpWithBackoff(); // Query now

            _ntpTimer.Elapsed += async (sender, args) => { await QueryNtpWithBackoff(); };
            _ntpTimer.AutoReset = true;
            _ntpTimer.Start();
        }

        private async Task QueryNtpWithBackoff(double maxDelaySeconds = 32)
        {
            var delay = TimeSpan.FromSeconds(1);
            while (true)
            {
                try
                {
                    await _ntpClient.QueryAsync();
                    Console.WriteLine($"NTP Time: {Timestamp}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NTP Query Failed. Trying again in {delay.TotalSeconds} seconds.\r\n{ex.Message}");
                    await Task.Delay(delay);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelaySeconds));
                }
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
