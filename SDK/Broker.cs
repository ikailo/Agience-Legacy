using GuerrillaNtp;
using Microsoft.IdentityModel.Tokens;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Formatter;
using MQTTnet.Diagnostics;
using GuerrillaNtp;
using Timer = System.Timers.Timer;
using System.Text.Json;

namespace Agience.SDK
{
    public class Broker
    {
        internal event Func<Task> Disconnected
        {
            add => _client.DisconnectedAsync += async (args) => await value();
            remove => _client.DisconnectedAsync -= async (args) => await value();
        }

        //TODO: Revise how the broker is initialized without breaking the Dependency Inversion Principle and without involving the host 
        NtpClient _ntpClient;
       
        public string? CustomNtpHost = null;

        //https://www.ntppool.org/zone/@
        List<string> ntpHosts = new() {
            "pool.ntp.org", 
            "north-america.pool.ntp.org",
            "europe.pool.ntp.org",
            "asia.pool.ntp.org",
            "south-america.pool.ntp.org",
            "africa.pool.ntp.org",
            "oceania.pool.ntp.org"};

        public string Timestamp => (_ntpClient?.Last ?? throw new InvalidOperationException()).Now.UtcDateTime.ToString(TIME_FORMAT);
                
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
                var messageType = Enum.TryParse<BrokerMessageType>(
                    args.ApplicationMessage.UserProperties.FirstOrDefault(x => x.Name == MESSAGE_TYPE_KEY)?.Value.ToString(), out var parsedMessageType) ?
                    parsedMessageType :
                    BrokerMessageType.UNKNOWN;

                var message = new BrokerMessage()
                {
                    Type = messageType,
                    Topic = args.ApplicationMessage.Topic //,
                    //Payload = args.ApplicationMessage.ConvertPayloadToString()
                };

                switch (messageType)
                {
                    case BrokerMessageType.EVENT:
                        message.Data = args.ApplicationMessage.ConvertPayloadToString();
                        break;
                    case BrokerMessageType.INFORMATION:
                        var payloadString = args.ApplicationMessage.ConvertPayloadToString();
                        message.Information = JsonSerializer.Deserialize<Information>(payloadString);
                        break;
                }

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

        internal async Task Subscribe(string topic, Func<BrokerMessage, Task> callback)
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

        internal void Publish(BrokerMessage message)
        {
            PublishAsync(message).ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    throw task.Exception;                    
                }
            }, TaskScheduler.Current);
        }

        internal async Task PublishAsync(BrokerMessage message)
        {
            if (_client.IsConnected)
            {
                string payload = string.Empty;

                switch (message.Type)
                {
                    case BrokerMessageType.EVENT:
                        payload = message.Data?.ToString() ?? string.Empty;
                        break;
                    case BrokerMessageType.INFORMATION:
                        payload = JsonSerializer.Serialize(message.Information);
                        break;
                }

                var mqMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(message.Topic ?? throw new ArgumentNullException(nameof(message.Topic)))
                    .WithPayload(payload)
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
            //Using a custom host from the settings, instead of the pre-defined list.
            if(!CustomNtpHost.IsNullOrEmpty())
            {
                ntpHosts.Clear();
                ntpHosts.Add(CustomNtpHost);
            }

            var delay = TimeSpan.FromSeconds(1);           
            var currentNtpHostIndex = 1;
            while (true)
            {
                var ntpHpst = ntpHosts[currentNtpHostIndex - 1];
                try
                {                    
                    _ntpClient = new(ntpHpst);
                    Console.WriteLine($"NTP Querying host {ntpHpst}");
                    _ntpClient.Query();
                    Console.WriteLine($"Connected to {ntpHpst}. NTP Time: {Timestamp}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NTP Query to host {ntpHpst} failed");
                  
                    var startNewCycle = currentNtpHostIndex == ntpHosts.Count();
                 
                    currentNtpHostIndex = startNewCycle ? 1 : currentNtpHostIndex + 1;

                    if(startNewCycle)
                    {
                        Console.WriteLine($"Trying again a NTP connection in {delay.TotalSeconds} seconds.\r\n{ex.Message}");
                        await Task.Delay(delay);
                        delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelaySeconds));
                    }
                }
            }
        }

    }

    internal class CallbackContainer
    {
        public Guid Id { get; } = Guid.NewGuid(); // Unique identifier for the callback
        public Func<BrokerMessage, Task> Callback { get; set; }
        public CallbackContainer(Func<BrokerMessage, Task> callback)
        {
            Callback = callback;
        }
    }
}
