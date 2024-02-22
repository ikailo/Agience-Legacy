using Agience.Model;
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
        internal ReadOnlyDictionary<string, string> TemplateDefaults => new(_templateDefaults);

        private readonly ConcurrentDictionary<string, (Model.Agent, DateTime)> _agents = new();
        private readonly ConcurrentDictionary<string, Model.Template> _templates = new();
        private Dictionary<string, string> _templateDefaults = new();
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

        private void SendWelcome(Model.Agent agent)
        {
            _agent.Runner.Log($"SendWelcome to {agent.Name} with {_agents.Values.Count} Agents and {_templates.Values.Count} Templates.");

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgentTopic(Id!, agent.Id!),
                Data = new Data
                {
                    { "type", "welcome" },
                    { "timestamp", _broker.Timestamp },
                    { "agency", JsonSerializer.Serialize(this.ToAgienceModel()) },
                    { "representative_id", RepresentativeId },
                    { "agents", JsonSerializer.Serialize(_agents.Values.Select(a => a.Item1).ToList()) },
                    { "agent_timestamps", JsonSerializer.Serialize(_agents.ToDictionary(a => a.Key, a => a.Value.Item2)) },
                    { "templates", JsonSerializer.Serialize(_templates.Values.ToList()) },
                    { "template_defaults", JsonSerializer.Serialize(_templateDefaults) }
                }
            });
        }

        private Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null) { return Task.CompletedTask; }

            // Incoming Agent Join message
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "join" &&
                message.Data?["agent"] != null &&
                message.Data?["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Data?["agent"]!);

                if (agent?.Id == message.SenderId && timestamp != null)
                {
                    ReceiveJoin(agent, (DateTime)timestamp);
                }
            }

            // Incoming Representative Claim message
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "representative_claim" &&
                message.Data?["agent"] != null &&
                message.Data?["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Data?["agent"]!);

                if (agent?.Id == message.SenderId && timestamp != null)
                {
                    ReceiveRepresentativeClaim(agent, (DateTime)timestamp);
                }
            }

            // Incoming Template message
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "template" &&
                message.Data?["template"] != null &&
                message.Data?["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var template = JsonSerializer.Deserialize<Model.Template>(message.Data?["template"]!);

                if (template?.AgentId == message.SenderId && timestamp != null)
                {
                    ReceiveTemplate(template);
                }
            }

            // Incoming Template Default message
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "template_default" &&
                message.Data?["default_name"] != null &&
                message.Data?["template_id"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var defaultName = message.Data?["default_name"]!;
                var templateId = message.Data?["template_id"]!;

                SetTemplateDefault(defaultName, templateId);               
            }
                  
            return Task.CompletedTask;
        }

        private void ReceiveJoin(Model.Agent modelAgent, DateTime timestamp)
        {
            _agent.Runner.Log($"ReceiveJoin {modelAgent.Name}");

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
                SendWelcome(modelAgent);
            }
        }

        internal void ReceiveWelcome(Model.Agency agency,
                                            string representativeId,
                                            List<Model.Agent> agents,
                                            Dictionary<string, DateTime> agentTimestamps,
                                            List<Model.Template> templates,
                                            Dictionary<string,string> templateDefaults,
                                            DateTime timestamp)
        {
            _agent.Runner.Log($"ReceiveWelcome from {agency.Name} {GetAgentName(representativeId)}");

            if (RepresentativeId != representativeId)
            {
                RepresentativeId = representativeId;
                _agent.Runner.Log($"Set Representative {GetAgentName(RepresentativeId)}");
            }

            foreach (var agent in agents)
            {
                _agents[agent.Id!] = (agent, agentTimestamps[agent.Id!]);
            }

            foreach (var template in templates)
            {   
                if (template.AgentId != _agent.Id)
                {
                    ReceiveTemplate(template);
                }
            }

            SetTemplateDefaults(templateDefaults);

            _agent.SendTemplatesToAgency();
        }

        // TODO: Handle race conditions
        // Network Latency, Simultaneous Joins, etc.
        private void ReceiveRepresentativeClaim(Model.Agent modelAgent, DateTime timestamp)
        {
            _agent.Runner.Log($"ReceiveRepresentativeClaim from {modelAgent.Name}");

            // TODO: Agent needs to relinquish default templates if they were previously the representative

            if (RepresentativeId != modelAgent.Id)
            {
                RepresentativeId = modelAgent.Id;
                _agent.Runner.Log($"Set Representative {GetAgentName(RepresentativeId)}");
            }

            if (_agent.Id == RepresentativeId)
            {
                var repJoinTime = _agents[_agent.Id!].Item2;
                foreach (var agent in _agents.Values.Where(a => a.Item2 >= repJoinTime).Select(a => a.Item1))
                {
                    SendWelcome(agent);
                }
            }
        }

        private void ReceiveTemplate(Model.Template modelTemplate)
        {   
            if (modelTemplate?.Id != null)
            {
                _agent.Runner.Log($"ReceiveTemplate {modelTemplate.Id} from {GetAgentName(modelTemplate.AgentId) ?? modelTemplate.AgentId ?? "null"}");
                
                _templates[modelTemplate.Id] = modelTemplate;
            }
        }

        private void SetTemplateDefault(string defaultName, string templateId)
        {
            _agent.Runner.Log($"SetTemplateDefault {defaultName} {templateId}");

            _templateDefaults[defaultName] = templateId;
        }

        private Model.Agency ToAgienceModel()
        {
            return new Model.Agency()
            {
                Id = Id,
                Name = Name
            };
        }

        internal void SetTemplateDefaults(Dictionary<string, string>? templateDefaults)
        {
            if (templateDefaults == null) { return; }

            foreach(var item in templateDefaults)
            {
                SetTemplateDefault(item.Key, item.Value);
            }            
        }

        public void SetTemplateDefault<T>(string name) where T : Template, new()
        {
            // TODO: Add constructor parameters

            _agent.Runner.Log($"SetTemplateDefault<T> {name} {typeof(T).FullName!}");

            _agent.SendTemplateDefaultToAgency(name, typeof(T).FullName!);
        }

        internal string? GetAgentName(string? agentId)
        {
            if (agentId == null) { return null; }

            return _agents.TryGetValue(agentId, out (Model.Agent, DateTime) agent) ? agent.Item1.Name : null;
        }
    }
}
