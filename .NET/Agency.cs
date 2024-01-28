
namespace Agience.Client
{
    public class Agency : Model.Agency
    {

        
        public new List<Agent> Agents { get; set; } = new();
        public bool IsConnected { get; internal set; }

        public event EventHandler<Message>? MessageReceived;

        private Authority _authority;

        public Agency(Authority authority)
        {
            _authority = authority;
        }

        internal async Task Connect(Broker broker)
        {
            await broker.SubscribeAsync($"+/{_authority.Id}/-/{Id}/-", ReceiveMessageCallback);

            IsConnected = true;
        }

        private Task ReceiveMessageCallback(Message message)
        {
            throw new NotImplementedException();
        }
    }
}
