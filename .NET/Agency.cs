namespace Agience.Client
{
    public class Agency
    {
        public event EventHandler<Message>? MessageReceived;

        public string? Id { get; set; }
        public string? Name { get; set; }        
        public List<Agent> Agents { get; set; } = new();        
        
        public Dictionary<string, Model.Template> Catalog { get; set; } // HERE
        
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
                await broker.SubscribeAsync($"+/{_authority.Id}/-/{Id}/-", ReceiveMessageCallback);
                _isSubscribed = true;
            }
        }

        internal async Task UnsubscribeAsync(Broker broker)
        {            
            if (_isSubscribed)
            {
                await broker.UnsubscribeAsync($"+/{_authority.Id}/-/{Id}/-");
                _isSubscribed = false;
            }
        }

        private Task ReceiveMessageCallback(Message message)
        {
            throw new NotImplementedException();
        }
    }
}
