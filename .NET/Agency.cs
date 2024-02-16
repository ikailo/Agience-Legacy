using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Agience.Client
{
    public class Agency
    {
        
        public string? Id { get; internal set; }
        public string? Name { get; internal set; }
        public bool IsConnected { get; private set; }
        internal string? RepresentativeId { get; private set; }
        public string Timestamp => _broker.Timestamp;
        internal ReadOnlyDictionary<string, Model.Template> Templates => new(_templates);
        //internal ReadOnlyDictionary<string, string> DefaultTemplates => new(_defaultTemplates);

        private readonly ConcurrentDictionary<string, (Model.Agent, DateTime)> _agents = new();
        private readonly ConcurrentDictionary<string, Model.Template> _templates = new();        
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly Agent _agent;

        internal Agency(Authority authority, Agent agent, Broker broker)
        {
            _authority = authority;
            _agent = agent;
            _broker = broker;
        }

        internal async Task Connect()
        {
            if (!IsConnected)
            {
                await _broker.Subscribe(_authority.AgencyTopic("+", Id!), _broker_ReceiveMessage);
                IsConnected = true;
            }
        }

        internal async Task Disconnect()
        {
            if (IsConnected)
            {
                await _broker.Unsubscribe(_authority.AgencyTopic("+", Id!));
                IsConnected = false;
            }
        }

        private async Task SendWelcome(Model.Agent agent)
        {
            // Add default templates to current templates

            _ = _agent.Runner.Log($"Sending welcome to {agent.Name} with {_agents.Values.Count} Agents and {_templates.Values.Count} Templates.");            
            
            await _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgentTopic(Id!, agent.Id!),
                Payload = new Data(new()
                {
                    { "type", "welcome" },
                    { "timestamp", _broker.Timestamp},
                    { "agency", JsonSerializer.Serialize(this.ToAgienceModel()) },
                    { "representative_id", RepresentativeId },
                    { "agents", JsonSerializer.Serialize(_agents.Values.Select(a => a.Item1).ToList()) },
                    { "agentTimestamps", JsonSerializer.Serialize(_agents.ToDictionary(a => a.Key, a => a.Value.Item2)) },
                    { "templates", JsonSerializer.Serialize(_templates.Values.ToList()) },
                    //{ "default_templates", JsonSerializer.Serialize(_defaultTemplates.Values.ToList()) },
                })
            }); ;
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload == null) { return; }

            // Incoming Agent Join message
            if (message.Type == MessageType.EVENT &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["type"] == "join" &&
                message.Payload["agent"] != null &&
                message.Payload["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Payload["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Payload["agent"]!);

                if (agent?.Id == message.SenderId && timestamp != null)
                {
                    await ReceiveJoin(agent, (DateTime)timestamp);
                }
            }

            // Incoming Representative Claim message
            if (message.Type == MessageType.EVENT &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["type"] == "representativeClaim" &&
                message.Payload["agent"] != null &&
                message.Payload["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Payload["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Payload["agent"]!);

                if (agent?.Id == message.SenderId && timestamp != null)
                {
                    await ReceiveRepresentativeClaim(agent, (DateTime)timestamp);
                }
            }

            // Incoming Template message
            if (message.Type == MessageType.EVENT &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["type"] == "template" &&
                message.Payload["template"] != null &&
                message.Payload["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Payload["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var template = JsonSerializer.Deserialize<Model.Template>(message.Payload["template"]!);

                if (template?.AgentId == message.SenderId && timestamp != null)
                {
                    ReceiveTemplate(template);
                }
            }

        }
        private async Task ReceiveJoin(Model.Agent modelAgent, DateTime timestamp)
        {
            _ = _agent.Runner.Log($"Received join from {modelAgent.Name}");

            // Add or update the Agent's timestamp
            if (_agents.TryGetValue(modelAgent.Id!, out (Model.Agent, DateTime) agent))
            {
                if (timestamp > agent.Item2)
                {
                    agent.Item2 = timestamp;
                }
            }
            else
            {
                _agents[modelAgent.Id!] = (modelAgent, timestamp);
            }

            if (_agent.Id == RepresentativeId)
            {
                await SendWelcome(modelAgent);
            }
        }

        internal async Task ReceiveWelcome(Model.Agency agency,
                                            string representativeId,
                                            List<Model.Agent> agents,
                                            Dictionary<string, DateTime> agentTimestamps,
                                            List<Model.Template> templates,
                                            DateTime timestamp)
        {
            _ = _agent.Runner.Log($"Received welcome from {agency.Name}");

            if (RepresentativeId != representativeId)
            {
                RepresentativeId = representativeId;
                _ = _agent.Runner.Log($"Set representative id {RepresentativeId}");
            }

            foreach (var agent in agents)
            {
                _agents[agent.Id!] = (agent, agentTimestamps[agent.Id!]);
            }

            foreach (var template in templates)
            {
                ReceiveTemplate(template);
            }

            await _agent.SendTemplatesToAgency();
        }

        // TODO: Handle race conditions
        // Network Latency, Simultaneous Joins, etc.
        private async Task ReceiveRepresentativeClaim(Model.Agent modelAgent, DateTime timestamp)
        {
            _ = _agent.Runner.Log($"Received representative claim from {modelAgent.Name}");

            if (RepresentativeId != modelAgent.Id)
            {
                RepresentativeId = modelAgent.Id;
                _ = _agent.Runner.Log($"Set representative id {RepresentativeId}");
            }

            if (_agent.Id == RepresentativeId)
            {
                var repJoinTime = _agents[_agent.Id!].Item2;
                foreach (var agent in _agents.Values.Where(a => a.Item2 >= repJoinTime).Select(a => a.Item1))
                {
                    await SendWelcome(agent);
                }
            }
        }

        private void ReceiveTemplate(Model.Template modelTemplate)
        {            
            // TODO: Remove templates when Agent leaves
            if (modelTemplate?.Id != null)
            {
                _templates[modelTemplate.Id] = modelTemplate;  // Replace whatever is there
                _ = _agent.Runner.Log($"Received template {modelTemplate.Id} from {modelTemplate.AgentId}");
            }
        }

        private Model.Agency ToAgienceModel()
        {
            return new Model.Agency()
            {
                Id = Id,
                Name = Name
            };
        }        
    }
}
