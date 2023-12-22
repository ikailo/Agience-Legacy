using Agience.Client.MQTT.Model;

namespace Agience.Client.MQTT
{
    public class Catalog
    {
        private readonly Identity _identity;

        public Catalog(Identity identity) {
            _identity = identity;
        }

        private Dictionary<string, Func<Agent?, Template>> _templateFactories = new();

        // TODO: Cache the templates by AgentId. Ensure only one instance of each template exists per agent.
        //private Dictionary<Agent, Dictionary<string, Template>> _templates = new(); // AgentId -> TemplateId -> Template

        public void Add(Func<Agent?, Template> templateFactory)
        {
            // Create a temporary instance to get the ID. 
            // TODO: If we're generating an instance, we should keep it so we don't have to generate it again.
            var tempInstance = templateFactory(null); 
            _templateFactories[tempInstance.Id] = templateFactory;
        }

        public void Add(Func<Template> templateFactory)
        {
            Add(agent => templateFactory());
        }

        public Template? GetTemplate(string id, Agent? agent = null)
        {
            
            if (_templateFactories.TryGetValue(id, out Func<Agent?, Template>? factory))
            {                
                return factory(agent);
            }
            return null;
        }

        internal Template? GetTemplate(string? templateId)
        {
            throw new NotImplementedException();
        }

        internal bool ContainsKey(string? templateId)
        {
            throw new NotImplementedException();
        }
    }

}
