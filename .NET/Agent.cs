using Agience.Model;
using System;

namespace Agience.Client
{
    public class Agent
    {
        public string? Id { get; internal set; }
        public string? Name { get; internal set; }
        public bool IsConnected { get; private set; }
        public Agency Agency => _agency;
        public IReadOnlyList<Template> Templates => _templates.Values.Select(t => t.Item1).ToList().AsReadOnly();


        private readonly Dictionary<string, (Template, OutputCallback?)> _templates = new();
        private readonly Authority _authority;        
        private readonly Agency _agency;
        private readonly Broker _broker;

        internal Agent(Authority authority, Agency agency, Broker broker)
        {
            _authority = authority;            
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
        public async Task<Data?> Dispatch(string templateId, Data? data = null)
        {
            throw new NotImplementedException();
        }

        // For when the template is not known
        public async Task<Data?> Prompt(string prompt, Data? data = null, string[]? outputKeys = null)
        {
            throw new NotImplementedException();
        }

    }
}