
using System.Collections.Concurrent;

namespace Agience.Client.MQTT.Model
{
    public class Agency : Agience.Model.Agency
    {

        // Agency
        // - Knows about its agents
        // - 
        public new List<Agent> Agents { get; set; } = new();

        public event EventHandler<Message>? MessageReceived;

        private readonly Identity _identity;
        private ConcurrentDictionary<string, DateTime> _knownAgents = new();

        public Agency(Identity identity)
        {
            _identity = identity;
        }

        private async Task Receive(Status? status)
        {
            if (status?.AgentId != null && status.AgentId != _identity.Id)
            {
                await Logger.Write($"{status.AgentId} status receive");

                if (!_knownAgents.ContainsKey(status.AgentId))
                {
                    await Send(new Status(_identity.Id), status.AgentId);
                }

                _knownAgents[status.AgentId] = DateTime.UtcNow;
                /*
                foreach (var template in _catalog.Values)
                {
                    if (template.AgentId == _identity.AgentId)
                    {
                        await Send(template, status.AgentId);
                    }
                }*/
            }
        }

    }
}
