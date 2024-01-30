namespace Agience.Client
{
    public class Agent //: Model.Agent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        internal new Instance? Instance { get; set; }
        public new Agency? Agency { get; internal set; }
        public bool IsConnected { get; internal set; }

        private Authority _authority;

        public Agent(Authority authority)
        {
            _authority = authority;
        }

        internal async Task Connect(Broker broker)
        {
            await broker.SubscribeAsync($"+/{_authority.Id}/-/-/{Id}", ReceiveMessageCallback);

            if (Agency != null && !Agency.IsConnected)
            {
                await Agency.Connect(broker);
            }

            IsConnected = true;
        }

        private async Task ReceiveMessageCallback(Message message)
        {
            throw new NotImplementedException();
        }

        // For when the template is local
        public async Task<Data?> Invoke<T>(Data? data = null) where T : Template, new()
        {
            var template = Instance?.Catalog.Retrieve<T>(this);

            if (template != null)
            {
                return await template.Process(data);
            }
            return null;
        }

        // For when the templateId is known. Local or remote.
        public async Task<Data?> Dispatch(string templateId, Data? data)
        {
            throw new NotImplementedException();
        }

        // For when the template is not known
        public async Task<Data?> Prompt(string prompt, Data? data)
        {
            throw new NotImplementedException();
        }
    }
}