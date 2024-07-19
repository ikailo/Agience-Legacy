using AutoMapper;
using Agience.SDK.Mappings;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using Agience.SDK.Models.Messages;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Entities.Agency), ReverseMap = true)]
    public class Agency : Models.Entities.Agency
    {
        //public string? Id { get; set; }
        //public string? Name { get; set; }
        public bool IsConnected { get; private set; }
        internal string? RepresentativeId { get; private set; }
        public string Timestamp => _broker.Timestamp;
        
        private readonly ConcurrentDictionary<string, (Models.Entities.Agent, DateTime)> _agents = new();        
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly Agent _agent; // TODO: Will need to be a list of Agents
        private readonly ILogger<Agency>? _logger;
        private readonly IMapper _mapper;

        internal Agency(Authority authority, Agent agent, Broker broker)
        {
            _authority = authority;
            _agent = agent;
            _broker = broker;            
            _mapper = AutoMapperConfig.GetMapper();
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

        private Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null) { return Task.CompletedTask; }

            // Incoming Agent Join message
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "join" &&
                message.Data?["agent"] != null &&
                message.Data?["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Models.Entities.Agent>(message.Data?["agent"]!);

                if (agent?.Id == message.SenderId && timestamp != null)
                {
                    ReceiveJoin(agent, (DateTime)timestamp);
                }
            }

            // Incoming Representative Claim message
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "representative_claim" &&
                message.Data?["agent"] != null &&
                message.Data?["timestamp"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Models.Entities.Agent>(message.Data?["agent"]!);

                if (agent?.Id == message.SenderId && timestamp != null)
                {
                    ReceiveRepresentativeClaim(agent, (DateTime)timestamp);
                }
            }

            /*
            // Incoming Context message // TODO: Should be History, not context
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "context" &&
                message.Data?["timestamp"] != null &&
                message.Data?["context"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var context = message.Data?["context"]!;

                ReceiveContext(context, timestamp);
            }*/

            return Task.CompletedTask;
        }

        private void ReceiveJoin(Models.Entities.Agent modelAgent, DateTime timestamp)
        {
            _logger?.LogInformation($"ReceiveJoin {modelAgent.Name}");

            // Add or update the Agent's timestamp
            if (_agents.TryGetValue(modelAgent.Id!, out (Models.Entities.Agent, DateTime) agent))
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

        private void SendWelcome(Models.Entities.Agent agent)
        {
            _logger?.LogInformation($"SendWelcome to {agent.Name} with {_agents.Values.Count} Agents.");

            _broker.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = _authority.AgentTopic(Id!, agent.Id!),
                Data = new Data
                {
                    { "type", "welcome" },
                    { "timestamp", _broker.Timestamp },
                    { "agency", JsonSerializer.Serialize(_mapper.Map<Models.Entities.Agency>(this)) },
                    { "representative_id", RepresentativeId },
                    { "agents", JsonSerializer.Serialize(_agents.Values.Select(a => a.Item1).ToList()) },
                    { "agent_timestamps", JsonSerializer.Serialize(_agents.ToDictionary(a => a.Key, a => a.Value.Item2)) }
                }
            });
        }

        internal void ReceiveWelcome(Models.Entities.Agency agency,
                                     string representativeId,
                                     List<Models.Entities.Agent> agents,
                                     Dictionary<string, DateTime> agentTimestamps,                                            
                                     DateTime timestamp)
        {
            _logger?.LogInformation($"ReceiveWelcome from {agency.Name} {GetAgentName(representativeId)}");

            if (RepresentativeId != representativeId)
            {
                RepresentativeId = representativeId;
                _logger?.LogInformation($"Set Representative {GetAgentName(RepresentativeId)}");
            }

            foreach (var agent in agents)
            {
                _agents[agent.Id!] = (agent, agentTimestamps[agent.Id!]);
            }
        }

        // TODO: Handle race conditions
        // Network Latency, Simultaneous Joins, etc.
        private void ReceiveRepresentativeClaim(Models.Entities.Agent modelAgent, DateTime timestamp)
        {
            _logger?.LogInformation($"ReceiveRepresentativeClaim from {modelAgent.Name}");            

            if (RepresentativeId != modelAgent.Id)
            {
                RepresentativeId = modelAgent.Id;
                _logger?.LogInformation($"Set Representative {GetAgentName(RepresentativeId)}");
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


        internal string? GetAgentName(string? agentId)
        {
            if (agentId == null) { return null; }

            return _agents.TryGetValue(agentId, out (Models.Entities.Agent, DateTime) agent) ? agent.Item1.Name : null;
        }
    }
}
