using Agience.Model;
using System.Runtime.InteropServices;
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
        public IReadOnlyList<Template> Templates => _templates.Values.Select(t => t.Item1).ToList().AsReadOnly();

        private readonly Dictionary<string, (Template, OutputCallback?)> _templates = new();
        private readonly Authority _authority;
        private readonly Agency _agency;
        private readonly Broker _broker;

        private Timer _representativeClaimTimer = new Timer(JOIN_WAIT);

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
            if (_agency.RepresentativeId != null) { return; }

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
            }); ;
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

        private async Task SendTemplatesToAgency()
        {
            foreach (var item in _templates.Values)
            {
                await SendTemplateToAgency(item.Item1.ToAgienceModel());
            }
        }



        private Model.Agent ToAgienceModel()
        {
            return new Model.Agent()
            {
                Id = Id,
                Name = Name
            };
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload == null) { return; }

            // Incoming Agency Welcome message
            if (message.Type == MessageType.EVENT &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["type"] == "welcome" &&
                message.Payload["agency"] != null &&
                message.Payload["timestamp"] != null &&
                message.Payload["agents"] != null &&
                message.Payload["templates"] != null)
            {
                var timestamp = DateTime.TryParse(message.Payload["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agency = JsonSerializer.Deserialize<Model.Agency>(message.Payload["agency"]!);
                var agents = JsonSerializer.Deserialize<List<Model.Agent>>(message.Payload["agents"]!);
                var agentTimestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(message.Payload["agentTimestamps"]!);
                var templates = JsonSerializer.Deserialize<List<Model.Template>>(message.Payload["templates"]!);

                if (agency?.Id == message.SenderId && agency.Id == _agency.Id && agents != null && agentTimestamps != null && templates != null && timestamp != null)
                {
                    await ReceiveWelcome(agency, agents, agentTimestamps, templates, (DateTime)timestamp);
                }
            }
        }

        private async Task ReceiveWelcome(Model.Agency agency,
                                            List<Model.Agent> agents,
                                            Dictionary<string, DateTime> agentTimestamps,
                                            List<Model.Template> templates,
                                            DateTime timestamp)
        {
            Console.WriteLine($"Received welcome from {agency.Name}");

            _agency.Add(agents, agentTimestamps);
            _agency.Add(templates);
            await SendTemplatesToAgency();
        }

        public Func<Task<Data?>, Task> Invoke(OutputCallback outputCallback)
        {
            return async task =>
            {
                var output = await task;
                await outputCallback(this, output);
            };
        }

        // For when the template is local
        public async Task<Data?> Invoke<T>(Data? data = null) where T : Template, new()
        {
            var templateId = typeof(T).FullName;

            if (string.IsNullOrEmpty(templateId))
            {
                return null;
            }

            if (_templates.TryGetValue(templateId, out (Template, OutputCallback?) templateAndCallback))
            {
                var (template, outputCallback) = templateAndCallback;

                var output = await template.Process(data);

                if (outputCallback != null)
                {
                    await outputCallback.Invoke(this, output);
                }

                return output;
            }

            return null;
        }

        // For when the templateId is known. Local or remote.
        public async Task<Data?> Dispatch(string templateId, Data? data = null)
        {
            throw new NotImplementedException();
        }

        // For when the template is not known
        public async Task<Data?> Prompt(string prompt, Data? data = null, string[]? outputKeys = null)
        {
            throw new NotImplementedException();
        }
    }
}