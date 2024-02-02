using Agience.Model;
using System;

namespace Agience.Client
{
    public class Agent
    {
        public string? Id { get; set; } // TODO: Make private?
        public string? Name { get; set; }
        public bool IsConnected { get; private set; }

        private readonly Dictionary<string, (Template, OutputCallback?)> _templates = new();
        private readonly Authority _authority;
        private readonly Instance _instance;
        private readonly Agency _agency;
        private readonly Broker _broker;

        public Agent(Authority authority, Instance instance, Agency agency, Broker broker)
        {
            _authority = authority;
            _instance = instance;
            _agency = agency;
            _broker = broker;
        }

        internal async Task Connect()
        {
            if (!IsConnected)
            {
                await _broker.Subscribe(_authority.AgentTopic("+", Id!), _broker_ReceiveMessage);

                //_agency.AddTemplates(_templates.Select(item => item.Value.Item1).ToList());
                //await _agency.Connect(); 
                
                IsConnected = true;
            }
        }

        internal async Task Disconnect()
        {
            if (IsConnected)
            {
                await _broker.Unsubscribe(_authority.AgentTopic("+", Id!));
                await _agency.Disconnect();                
                IsConnected = false;
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
                var output = await task;
                await outputCallback(this, output);
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