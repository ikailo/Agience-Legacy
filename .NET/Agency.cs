
using Agience.Model;

namespace Agience.Client
{
    public class Agency : Model.Agency
    {
        public delegate Task ConnectedEventArgs(Agency agency);
        public event ConnectedEventArgs? Connected;

        public event EventHandler<Message>? MessageReceived;
        
        public new List<Agent> Agents { get; set; } = new();
        public bool IsConnected { get; internal set; }
        public Dictionary<string, Model.Template> Catalog { get; set; }

        private Dictionary<string, Model.Template> _templates = new();
        private Authority _authority;

        public Agency(Authority authority)
        {
            _authority = authority;
        }

        internal async Task Connect(Broker broker)
        {
            await broker.SubscribeAsync($"+/{_authority.Id}/-/{Id}/-", ReceiveMessageCallback);

            IsConnected = true;

            if (Connected != null)
            {
                await Connected.Invoke(this);
            }
        }

        /*
        // Method to add a template (local or remote)
        public void AddTemplate(Model.Template template)
        {
            _templates[template.Id] = template;
        }

        // Method to get a template by ID
        public Model.Template? GetTemplate(string templateId, string localAgentId)
        {
            if (_templates.TryGetValue(templateId, out Model.Template? template))
            {
                if (template.AgentId == localAgentId || string.IsNullOrEmpty(template.AgentId))
                {
                    // Template is local or the same agent's; process locally
                    return template;
                }
                else
                {                    
                    throw new InvalidOperationException($"Template {templateId} must be processed by remote agent {template.AgentId}");
                }
            }

            return null; // Template not found
        }*/

        private Task ReceiveMessageCallback(Message message)
        {
            throw new NotImplementedException();
        }
    }
}
