
using Agience.Model;

namespace Agience.Client
{
    public class Agency //: Model.Agency
    {
        public event EventHandler<Message>? MessageReceived;

        public string? Id { get; set; }
        public string? Name { get; set; }
        
        public new List<Agent> Agents { get; set; } = new();
        public bool IsSubscribed { get; private set; }
        public Dictionary<string, Model.Template> Catalog { get; set; }

        private Dictionary<string, Model.Template> _templates = new();
        private Authority _authority;

        public Agency(Authority authority)
        {
            _authority = authority;
        }

        internal async Task SubscribeAsync(Broker broker)
        {
            if (!IsSubscribed)
            {
                await broker.SubscribeAsync($"+/{_authority.Id}/-/{Id}/-", ReceiveMessageCallback);
                IsSubscribed = true;
            }
        }

        internal async Task UnsubscribeAsync(Broker broker)
        {            
            if (IsSubscribed)
            {
                await broker.UnsubscribeAsync($"+/{_authority.Id}/-/{Id}/-");
                IsSubscribed = false;
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
