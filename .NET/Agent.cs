namespace Agience.Client
{
    public class Agent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        internal new Instance? Instance { get; set; }
        public new Agency? Agency { get; internal set; }
        public bool IsSubscribed { get; private set; }

        private Authority _authority;

        public Agent(Authority authority)
        {
            _authority = authority;
        }

        internal async Task SubscribeAsync(Broker broker)
        {
            if (!IsSubscribed)
            {
                await broker.SubscribeAsync($"+/{_authority.Id}/-/-/{Id}", ReceiveMessageCallback);

                if (Agency != null)
                {
                    await Agency.SubscribeAsync(broker);
                }

                IsSubscribed = true;
            }
        }

        internal async Task UnsubscribeAsync(Broker broker)
        {
            if (IsSubscribed)
            {
                await broker.UnsubscribeAsync($"+/{_authority.Id}/-/-/{Id}");

                if (Agency != null)
                {
                    await Agency.UnsubscribeAsync(broker);
                }

                IsSubscribed = false;
            }
        }

        private async Task ReceiveMessageCallback(Message message)
        {
            throw new NotImplementedException();
        }

        public Func<Task<Data?>, Task> Invoke(Func<Agent, Data?, Task> method)
        {
            return async task =>
            {
                var result = await task;
                await method(this, result);
            };
        }

        // For when the template is local
        public async Task<Data?> Invoke<T>(Data? data = null) where T : Template, new()
        {
            var result = Instance?.Templates.Retrieve<T>();

            if (result.HasValue)
            {
                var (template, callback) = result.Value;

                template.Agent = this;
                
                var output = await template.Process(data);

                if (callback != null)
                {
                   await callback.Invoke(this, output);
                }

                return output;
            }

            return null;
        }

        // For when the templateId is known. Local or remote.
        public async Task<Data?> Dispatch(string templateId, Data? data)
        {
            throw new NotImplementedException();
        }

        // For when the template is not known
        public async Task<Data?> Prompt(Data? data, string[]? outputKeys = null)
        {
            throw new NotImplementedException();
        }

    }
}