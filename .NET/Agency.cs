using System.Text.Json;

namespace Agience.Client
{
    public class Agency
    {
        public event EventHandler<Message>? MessageReceived;

        public string? Id { get; internal set; }
        public string? Name { get; internal set; }
        public bool IsConnected { get; private set; }

        private readonly List<Agent> _agents = new();
        private readonly Dictionary<string, Model.Template> _templates = new();        
        private readonly Authority _authority;
        private readonly Broker _broker;        

        internal Agency(Authority authority, Broker broker)
        {
            _authority = authority;
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

        private async Task SendTemplates(Broker broker, List<Model.Template> templates)
        {
            await broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AuthorityTopic(Id!),
                Payload = new Data(new()
                {
                    { "type", "broadcastTemplates" },
                    { "timestamp", broker.Timestamp},
                    { "instance", JsonSerializer.Serialize(templates) }
                })
            }); ;
        }

        private Task _broker_ReceiveMessage(Message message)
        {
            throw new NotImplementedException();
        }

        private Task ReceiveTemplates(Broker broker, Catalog? templates)
        {
            throw new NotImplementedException();
        }

        internal void AddTemplates(List<Template> templates)
        {
            throw new NotImplementedException();
        }
    }
}
