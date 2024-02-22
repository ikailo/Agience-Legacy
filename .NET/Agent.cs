using Agience.Model;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace Agience.Client
{
    public class Agent
    {
        private const int JOIN_WAIT = 5000;
        public string? Id { get; internal set; }
        public string? Name { get; internal set; }
        public bool IsConnected { get; private set; }
        public Agency Agency => _agency;
        public string Timestamp => _broker.Timestamp;
        public History History { get; } = new(); // TODO: Make History ReadOnly for external access

        internal IReadOnlyDictionary<string, (Template, OutputCallback?)> Templates => new ReadOnlyDictionary<string, (Template, OutputCallback?)>(_templates);

        private readonly Dictionary<string, (Template, OutputCallback?)> _templates = new();        
        private readonly ConcurrentDictionary<string, Runner> _informationCallbacks = new();
        private readonly Timer _representativeClaimTimer = new Timer(JOIN_WAIT);
        private readonly Authority _authority;
        private readonly Agency _agency;
        private readonly Broker _broker;        

        // Returns the top-level runner. Entry point for new information (without parent) processing. 
        // TODO: Need to figure out how to handle multilevel threaded messaging.
        private Runner? _runner;
        public Runner Runner
        {
            get
            {
                // TODO: Should this be a new runner each time?
                return _runner == null ? new Runner(this) : _runner;
            }
        }

        internal Agent(Authority authority, Broker broker, Model.Agency modelAgency)
        {
            _authority = authority;
            _broker = broker;
            _agency = new Agency(authority, this, broker)
            {
                Id = modelAgency.Id,
                Name = modelAgency.Name
            };

            _representativeClaimTimer.AutoReset = false;
            _representativeClaimTimer.Elapsed += (s, e) => SendRepresentativeClaim();
        }

        internal async Task Connect()
        {
            if (!IsConnected)
            {
                await _broker.Subscribe(_authority.AgentTopic("+", Id!), _broker_ReceiveMessage);
                await _agency.Connect();
                IsConnected = true;
            }
            SendJoin();
            _representativeClaimTimer.Start();
        }

        internal async Task Disconnect()
        {
            if (IsConnected)
            {
                await _broker.Unsubscribe(_authority.AgentTopic("+", Id!));
                // TODO: Need to let the Agency know our templates are no longer available
                await _agency.Disconnect();
                IsConnected = false;
            }
        }

        private void SendJoin()
        {
            Runner.Log("SendJoin");

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "join" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(this.ToAgienceModel()) },
                    { "random", new Random().NextInt64().ToString() }
                }
            });
        }

        private void SendRepresentativeClaim()
        {
            if (_agency.RepresentativeId != null) { return; } // Was set by another agent

            // Take ownership of the default templates
            // TODO: Not sure if this is the best way to do this
            
            Runner.Log("SendRepresentativeClaim");

            AddTemplate(new(new Templates.Default.Log() { Agent = this }, null));
            AddTemplate(new(new Templates.Default.Context() { Agent = this }, null));
            AddTemplate(new(new Templates.Default.Debug() { Agent = this }, null));
            AddTemplate(new(new Templates.Default.Echo() { Agent = this }, null));
            AddTemplate(new(new Templates.Default.History() { Agent = this }, null));            
            AddTemplate(new(new Templates.Default.Prompt() { Agent = this }, null));

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "representative_claim" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(this.ToAgienceModel()) },
                }
            });
        }

        private void SendTemplateToAgency(Model.Template template)
        {
            Runner.Log($"SendTemplateToAgency {template.Id}");

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "template" },
                    { "timestamp", _broker.Timestamp},
                    { "template", JsonSerializer.Serialize(template) }
                }
            });
        }

        internal void SendTemplateDefaultToAgency(string defaultName, string templateId)
        {
            Runner.Log($"SendTemplateDefaultToAgency {defaultName} {defaultName}");

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "template_default" },
                    { "timestamp", _broker.Timestamp},
                    { "default_name", defaultName },
                    { "template_id", templateId }
                }
            });
        }

        internal void SendInformationToAgent(Information information, string targetAgentId, Runner? runner = null)
        {
            //Runner.Log("SendInformationToAgent"); //Stack overflow

            if (runner != null)
            {
                _informationCallbacks[information.Id!] = runner;
            }

            _broker.Publish(new Message()
            {
                Type = MessageType.INFORMATION,
                Topic = _authority.AgentTopic(Id!, targetAgentId),
                Information = information
            });
        }

        internal void AddTemplates(List<(Template, OutputCallback?)> templates)
        {
            foreach (var (template, callback) in templates)
            {
                AddTemplate((template, callback));
            }
        }

        internal void AddTemplate((Template, OutputCallback?) template)
        {
            // TODO: Duplicate templates could be an issue.  Maybe need versioning.
            _templates[template.Item1.Id!] = template;

            if (IsConnected)
            {
                SendTemplateToAgency(template.Item1.ToAgienceModel());
            }
        }

        internal void SendTemplatesToAgency()
        {
            foreach (var item in _templates.Values)
            {
                SendTemplateToAgency(item.Item1.ToAgienceModel());
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || (message.Data == null && message.Information == null)) { return; }

            // Incoming Agency Welcome message
            if (message.Type == MessageType.EVENT &&                
                message.Data?["type"] == "welcome" &&
                message.Data?["agency"] != null &&
                message.Data?["representative_id"] != null &&
                message.Data?["timestamp"] != null &&
                message.Data?["agents"] != null &&
                message.Data?["templates"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agency = JsonSerializer.Deserialize<Model.Agency>(message.Data?["agency"]!);
                var representativeId = message.Data?["representative_id"]!;
                var agents = JsonSerializer.Deserialize<List<Model.Agent>>(message.Data?["agents"]!);
                var agentTimestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(message.Data?["agent_timestamps"]!);
                var templates = JsonSerializer.Deserialize<List<Model.Template>>(message.Data?["templates"]!);
                var templateDefaults = JsonSerializer.Deserialize<Dictionary<string, string>>(message.Data?["template_defaults"]!);


                if (agency?.Id == message.SenderId && agency.Id == _agency.Id && agents != null && 
                    agentTimestamps != null && templates != null && timestamp != null && templateDefaults != null)
                {
                    _agency.ReceiveWelcome(agency, representativeId, agents, agentTimestamps, templates, templateDefaults, (DateTime)timestamp);
                }
            }

            // Incoming Agent Information message
            if (message.Type == MessageType.INFORMATION &&
                message.Information != null)
            {
                await ReceiveInformation(message.Information);
            }
        }        

        private async Task ReceiveInformation(Information information)
        {
            // Runner.Log($"ReceiveInformation {information.Id}"); // Stack Overflow

            if (information.InputAgentId == Id)
            {
                // This is returned information
                if (_informationCallbacks.TryRemove(information.Id!, out Runner? runner))
                {
                    runner.ReceiveOutput(information);
                }
            }

            if (information.OutputAgentId == null)
            {
                // This is information that needs to be processed. Presumably Local. Dispatch it.
                await new Runner(this, information).DispatchAsync();

                // Return the output to the input agent
                SendInformationToAgent(information, information?.InputAgentId!);
            }
        }

        internal Model.Agent ToAgienceModel()
        {
            return new Model.Agent()
            {
                Id = Id,
                Name = Name
            };
        }
    }
}