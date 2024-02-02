using Agience.Model;
using System;

namespace Agience.Client
{
    public class Agent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        internal new Instance? Instance { get; set; }
        public new Agency? Agency { get; internal set; }
        public bool IsSubscribed { get; private set; }

        private Dictionary<string, (Template, OutputCallback?)> _templates = new();      

        private Authority _authority;

        public Agent(Authority authority)
        {
            _authority = authority;
        }

        internal async Task ConnectAsync(Broker broker)
        {            
            await SubscribeAsync(broker);
        }

        internal async Task SubscribeAsync(Broker broker)
        {
            if (!IsSubscribed)
            {
                await broker.SubscribeAsync(_authority.AgentTopic("+", Id!), _broker_ReceiveMessage);

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
                await broker.UnsubscribeAsync(_authority.AgentTopic("+", Id!));

                if (Agency != null)
                {
                    await Agency.UnsubscribeAsync(broker);
                }

                IsSubscribed = false;
            }
        }

        internal void AddTemplate((Template, OutputCallback?) template)
        {
            _templates[template.Item1.Id!] = template;
        }

        internal void AddTemplates(List<(Template, OutputCallback?)> templates)
        {
            foreach (var (template, callback) in templates)
            {
                AddTemplate((template, callback));
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public Func<Task<Data?>, Task> Invoke(OutputCallback outputCallback)
        {
            return async task =>
            {
                var result = await task;
                await outputCallback(this, result);
            };
        }

        // For when the template is local
        public async Task<Data?> Invoke<T>(Data? data = null) where T : Template, new()
        {
            var templateId = typeof(T).FullName;

            if (string.IsNullOrEmpty(templateId))
            {
                return null;
            }

            if (_templates.TryGetValue(templateId, out (Template, OutputCallback?) templateAndCallback))
            {
                var (template, outputCallback) = templateAndCallback;

                var output = await template.Process(data);

                if (outputCallback != null)
                {
                    await outputCallback.Invoke(this, output);
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