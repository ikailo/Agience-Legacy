using MQTTnet.Client;
using System.Collections.Concurrent;
using Timer = System.Timers.Timer;

namespace Technologai
{
    public class Agent
    {
        public event EventHandler<string>? LogMessage;
        public delegate Task OutputCallback(Data? output);

        const string LOG_MESSAGE_TEMPLATE_ID = "monitor.display_message";

        public string? Name => Identity.Name;
        public string Id => Identity.AgentId;

        public Identity Identity { get; private set; }
        public Catalog Catalog { get; private set; }
        public Context Context { get; private set; }

        private ConcurrentDictionary<string, OutputCallback> _outputCallbacks = new();
        private ConcurrentDictionary<string, DateTime> _knownAgents = new();
        private MqttClient _mqtt;
        private Timer? _killTimer;

        public Agent(string authority, string instanceId, string instanceSecret, string agentId)
        {
            Identity = new Identity(authority, instanceId, instanceSecret, agentId);

            Catalog = new Catalog(Identity);
            Context = new Context(Identity);

            _mqtt = new MqttClient(Identity, _mqtt_MessageReceived);
        }

        // ** TRANSPORT **

        private async void _mqtt_MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs args)
        {
            var brokerMessage = BrokerMessage.FromMqttArgs(args);

            switch (brokerMessage.MessageType)
            {
                case AgentMessageType.PULSE:
                    await Receive(brokerMessage.MessageData as Pulse);
                    break;
                case AgentMessageType.TEMPLATE:
                    await Receive(brokerMessage.MessageData as Template);
                    break;
                case AgentMessageType.INFORMATION:
                    await Receive(brokerMessage.MessageData as Information);
                    break;
            }
        }

        private async Task Receive(Pulse? pulse)
        {
            if (pulse != null && !string.IsNullOrEmpty(pulse.MemberId) && pulse.MemberId != Id)
            {
                
                await WriteLog($"{pulse?.MemberId} pulse receive");

                if (!_knownAgents.ContainsKey(pulse.MemberId))
                {
                    await Send(new Pulse(Id), pulse.MemberId);
                }

                _knownAgents[pulse.MemberId] = DateTime.UtcNow;

                foreach (var template in Catalog.Values)
                {
                    if (template.MemberId == Id)
                    {
                        await Send(template, pulse.MemberId);
                    }
                }
            }
        }

        private async Task Receive(Template? template)
        {
            if (template != null && template.MemberId != Id)
            {
                await WriteLog($"{template.MemberId} {template.Id} template receive");

                Catalog.Add(template);
            }
        }

        private async Task Receive(Information? information)
        {
            if (information == null) { return; }

            information.Agent = this;

            Context.Add(information); // TODO? only update if newer

            // Closed and this agent is the creator
            if (information.InformationState == InformationState.CLOSED && information.CreatorId == Id)
            {
                // Invoke the callback.
                if (_outputCallbacks.Remove(information.Id, out OutputCallback? callback))
                {
                      await callback.Invoke(information.Output);
                }

                // Assess the publisher information
                var publisherInformation = Context.GetPublisher(information.Id);

                if (publisherInformation == null)
                {
                    // This is a root request. 
                    return;
                }

                information = publisherInformation;

                // Fall through to next if condition so the publisher can be assessed and processed               
            }

            // Open, and this agent is assigned
            if (information.InformationState == InformationState.OPEN && information.WorkerId == Id)
            {
                // TODO: Debounce

                if (await information.Assess())
                {
                    // TODO: Exception Handling
                    await information.Process();
                }
            }

            // Closed, and this agent is not the creator
            if (information.InformationState == InformationState.CLOSED && information.CreatorId != Id)
            {
                // TODO: Review. Add to Context.
            }
        }

        private async Task Send(Pulse pulse, string toMemberId = "0")
        {
            await WriteLog($"{toMemberId} {pulse.MemberId} pulse send");

            await Send(AgentMessageType.PULSE, pulse, toMemberId);
        }

        public async Task Send(Template template, string toMemberId)
        {
            await WriteLog($"{toMemberId} {template.Id} template send");

            await Send(AgentMessageType.TEMPLATE, template, toMemberId);
        }

        public async Task Send(Information information, string toMemberId)
        {
            // TODO: Review this. Uncommenting causes stack-overflow.
            // await WriteLog($"{toMemberId} {information.Id} information send");

            await Send(AgentMessageType.INFORMATION, information, toMemberId);
        }

        public async Task Send(AgentMessageType messageType, object? messageData, string toMemberId = "0")
        {
            var brokerMessage = new BrokerMessage(Identity)
            {
                MessageType = messageType,
                MessageData = messageData,
                ToMemberId = toMemberId
            };

            string messageJson = brokerMessage.ConvertMessageDataToString();

            await _mqtt.PublishAsync(brokerMessage.Topic, messageJson, brokerMessage.MessageType);
        }

        // This Publish method returns the output of the information. It will await until the information is closed and the output is available. Better for short running processes.
        internal async Task<Data?> Publish(Information information)
        {
            bool callbackComplete = false;

            Data? result = null;

            await PublishAsync(information, 
                    (output) =>
                {
                    result = output;
                    callbackComplete = true;
                    return Task.CompletedTask;
                }
            );

            // FIXME TODO: This can wait indefinitly if the information is never closed or template doesn't exist. Add timeout / decay / cancellation token.

            while (!callbackComplete)
            {
                await Task.Delay(10);
            }

            return result;
        }

        /*
        public async Task PublishAsync(OutputCallback? callback, string? instruction, Data? input = null)
        {
            await PublishAsync(new Information(this, templateId, input), callback);
        }*/

        // This PublishAsync method publishes information immediately and stores a callback to be invoked when the information is closed. Better for long running processes.
        public async Task PublishAsync(string templateId, OutputCallback? callback, Data? input = null)
        {
            await PublishAsync(new Information(this, templateId, input), callback);
        }

        public async Task PublishAsync(Information information, OutputCallback? callback)
        {
            if (information.TemplateId != LOG_MESSAGE_TEMPLATE_ID)
            {
                await WriteLog($"{information.Id} Publish> {information.TemplateId} | {information.InformationState} | {information.Input} | {information.Output}");
            }

            if (callback != null)
            {
                _outputCallbacks[information.Id] = callback;
            }

            if (information.InformationState == InformationState.DRAFT)
            {
                information.InformationState = InformationState.OPEN;
            }

            if (information.WorkerId == null)
            {
                information.WorkerId = Catalog.ContainsKey(information.TemplateId) ? Catalog[information.TemplateId].MemberId : Id;
            }

            // Short circuit
            if (information.WorkerId == null || information.WorkerId == Id)
            {
                new Task(async () =>
                {
                    await Receive(information);
                }).Start();
                return;
            }

            // Long route
            await Send(information, information.WorkerId);
        }

        public async Task WriteLog(string message)
        {
            if (_mqtt.IsConnected && Catalog.ContainsKey(LOG_MESSAGE_TEMPLATE_ID) && Catalog[LOG_MESSAGE_TEMPLATE_ID].MemberId != null && Catalog[LOG_MESSAGE_TEMPLATE_ID].MemberId != Id)
            {
                await PublishAsync(LOG_MESSAGE_TEMPLATE_ID, null, $"{Name?.PadRight(21)} | {message}");
            }
            else
            {
                LogMessage?.Invoke(this, message);
            }
        }

        // Startup

        public async Task Start()
        {
            // Hack
            // TODO: Fix per AI-17            
            await WriteLog($"Warming up...");
            await new HttpClient().GetAsync($"{Identity.Authority.AuthUri}/.well-known/jwks.json");
            await new HttpClient().GetAsync($"{Identity.Authority.AuthUri}/.well-known/openid-configuration");
            // End Hack

            await Identity.Authenticate(Identity.Authority.BrokerUri);

            await WriteLog($"Authenticated");

            await _mqtt.ConnectAsync();

            await WriteLog($"Connected");

            await _mqtt.SubscribeAsync(Identity.SubscribeMemberMask);

            await WriteLog($"Subscribed {Identity.SubscribeMemberMask}");

            await _mqtt.SubscribeAsync(Identity.SubscribeAgencyMask);

            await WriteLog($"Subscribed {Identity.SubscribeAgencyMask}");

            await Send(new Pulse(Id));

            await Task.Delay(5000); // Wait here for a bit to sync up Templates
        }

        public async Task Stop()
        {
            await _mqtt.DisconnectAsync();
        }

        public async void Kill(double delayMs = 0)
        {
            if (delayMs == 0)
            {
                await KillTimerCallback();
            }
            else
            {
                _killTimer = new Timer(delayMs);
                _killTimer.Elapsed += async (sender, e) => await KillTimerCallback();
                _killTimer.Start();
            }
        }

        private async Task KillTimerCallback()
        {
            await _mqtt.DisconnectAsync();
            Environment.Exit(0);
        }
    }
}