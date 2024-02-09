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

        internal IReadOnlyDictionary<string, (Template, OutputCallback?)> Templates => new ReadOnlyDictionary<string, (Template, OutputCallback?)>(_templates);

        private readonly Dictionary<string, (Template, OutputCallback?)> _templates = new();
        private readonly Authority _authority;
        private readonly Agency _agency;
        private readonly Broker _broker;

        private Timer _representativeClaimTimer = new Timer(JOIN_WAIT);

        // Returns the top-level runner. Entry point for new information (without parent) processing. 
        // TODO: Need to figure out how to handle multilevel threaded messaging.
        private Runner? _runner;
        public Runner Runner
        {
            get
            {
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
            _representativeClaimTimer.Elapsed += async (s, e) => await SendRepresentativeClaim();
        }

        internal async Task Connect()
        {
            if (!IsConnected)
            {
                await _broker.Subscribe(_authority.AgentTopic("+", Id!), _broker_ReceiveMessage);
                await _agency.Connect();
                IsConnected = true;
            }
            await SendJoin();
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

        private async Task SendJoin()
        {
            await _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Payload = new Data(new()
                {
                    { "type", "join" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(this.ToAgienceModel()) },
                    { "random", new Random().NextInt64().ToString() }
                })
            });
        }

        private async Task SendRepresentativeClaim()
        {
            if (_agency.RepresentativeId != null) { return; } // Was set by another agent

            await _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Payload = new Data(new()
                {
                    { "type", "representativeClaim" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(this.ToAgienceModel()) },
                })
            });
        }

        private async Task SendTemplateToAgency(Model.Template template)
        {
            await _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Payload = new Data(new()
                {
                    { "type", "template" },
                    { "timestamp", _broker.Timestamp},
                    { "template", JsonSerializer.Serialize(template) }
                })
            });
        }

        internal async Task SendInformationToAgent(Information information, string targetAgentId)
        {
            await _broker.Publish(new Message()
            {
                Type = MessageType.INFORMATION,
                Topic = _authority.AgentTopic(Id!, targetAgentId),
                Payload = new Data(new()
                {
                    {"information",JsonSerializer.Serialize(information) } // FIXME: Too much serialization
                })
            });
        }

        internal async Task AddTemplates(List<(Template, OutputCallback?)> templates)
        {
            foreach (var (template, callback) in templates)
            {
                await AddTemplate((template, callback));
            }
        }

        internal async Task AddTemplate((Template, OutputCallback?) template)
        {
            // TODO: Duplicate templates could be an issue.  Maybe need versioning.
            _templates[template.Item1.Id!] = template;

            if (IsConnected)
            {
                await SendTemplateToAgency(template.Item1.ToAgienceModel());
            }
        }

        internal async Task SendTemplatesToAgency()
        {
            foreach (var item in _templates.Values)
            {
                await SendTemplateToAgency(item.Item1.ToAgienceModel());
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload == null) { return; }

            // Incoming Agency Welcome message
            if (message.Type == MessageType.EVENT &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["type"] == "welcome" &&
                message.Payload["agency"] != null &&
                message.Payload["representative_id"] != null &&
                message.Payload["timestamp"] != null &&
                message.Payload["agents"] != null &&
                message.Payload["templates"] != null)
            {
                var timestamp = DateTime.TryParse(message.Payload["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agency = JsonSerializer.Deserialize<Model.Agency>(message.Payload["agency"]!);
                var representativeId = message.Payload["representative_id"]!;
                var agents = JsonSerializer.Deserialize<List<Model.Agent>>(message.Payload["agents"]!);
                var agentTimestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(message.Payload["agentTimestamps"]!);
                var templates = JsonSerializer.Deserialize<List<Model.Template>>(message.Payload["templates"]!);

                if (agency?.Id == message.SenderId && agency.Id == _agency.Id && agents != null && agentTimestamps != null && templates != null && timestamp != null)
                {
                    await _agency.ReceiveWelcome(agency, representativeId, agents, agentTimestamps, templates, (DateTime)timestamp);
                }
            }

            // Incoming Agent Information message
            if (message.Type == MessageType.INFORMATION &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["information"] != null
                )
            {
                var information = JsonSerializer.Deserialize<Information>(message.Payload["information"]!);

                if (information != null)
                {
                    await ReceiveInformation(information);
                }
            }
        }

        private async Task ReceiveInformation(Information information)
        {
            if (information.InputAgentId == Id)
            {
                Console.WriteLine($"returned: {information}");
                // This is returned information
                // Find the runner that is waiting for this information
            }

            if (information.OutputAgentId == null)
            {
                // This is information that needs to be processed

                _ = await new Runner(this, information).Dispatch();

                await SendInformationToAgent(information, information?.InputAgentId!);
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