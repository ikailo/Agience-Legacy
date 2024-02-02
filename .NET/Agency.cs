using System.Text.Json;

namespace Agience.Client
{
    public class Agency
    {
        public event EventHandler<Message>? MessageReceived;

        public string? Id { get; set; }
        public string? Name { get; set; }        
        public List<Agent> Agents { get; set; } = new();        
        
        private Dictionary<string, Model.Template> _templates { get; set; }
        
        private Authority _authority;
        private bool _isSubscribed;

        public Agency(Authority authority)
        {
            _authority = authority;
        }

        internal async Task SubscribeAsync(Broker broker)
        {
            if (!_isSubscribed)
            {
                await broker.SubscribeAsync(_authority.AgencyTopic("+", Id!), _broker_ReceiveMessage);
                _isSubscribed = true;
            }
        }

        internal async Task UnsubscribeAsync(Broker broker)
        {            
            if (_isSubscribed)
            {
                await broker.UnsubscribeAsync(_authority.AgencyTopic("+", Id!));
                _isSubscribed = false;
            }
        }

        internal async Task SendTemplates(Broker broker, List<Model.Template> templates)
        {
            await broker.PublishAsync(new Message()
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

        internal Task ReceiveTemplates(Broker broker, Catalog? templates)
        {
            throw new NotImplementedException();
        }


        private Task _broker_ReceiveMessage(Message message)
        {
            throw new NotImplementedException();
        }


    }
}
