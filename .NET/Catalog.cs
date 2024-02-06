using System.Collections;

namespace Agience.Client
{
    public delegate Task OutputCallback(Agent agent, Data? data);

    internal class Catalog : IEnumerable<string>
    {
        private readonly Dictionary<string, Type> _types = new();
        private readonly Dictionary<string, OutputCallback> _outputCallbacks = new();

        internal void Add<T>(OutputCallback? outputCallback = null) where T : Template, new()
        {
            Type type = typeof(T);

            if (string.IsNullOrEmpty(type.FullName))
            {
                throw new ArgumentNullException(nameof(type.FullName));
            }

            _types[type.FullName] = typeof(T);

            if (outputCallback != null)
            {
                _outputCallbacks[typeof(T).FullName!] = outputCallback;
            }
        }

        internal void Remove<T>() where T : Template, new()
        {
            Type type = typeof(T);

            if (string.IsNullOrEmpty(type.FullName))
            {
                throw new ArgumentNullException(nameof(type.FullName));
            }

            _types.Remove(type.FullName!);
            _outputCallbacks.Remove(type.FullName!);
        }

        internal (Template, OutputCallback?)? GetTemplateForAgent(string templateId, Agent agent)
        {
            (Template, OutputCallback?)? template = Retrieve(templateId);

            if (template.HasValue)
            {
                template.Value.Item1.Agent = agent;
                return template;
            }
            return null;
        }

        internal List<(Template, OutputCallback?)> GetTemplatesForAgent(Agent agent)
        {
            // TODO: It's likely that some Agents will want to instantiate at runtime instead of during connection.
            // TODO: Add another method to retrive a factory for a template.

            List<(Template, OutputCallback?)> templates = new();

            foreach (var type in _types.Values)
            {
                var template = GetTemplateForAgent(type.FullName!, agent);

                if (template.HasValue)
                {
                    templates.Add(template.Value);
                }
            }
            return templates;            
        }

        private (Template, OutputCallback?)? Retrieve(string? templateId)
        {
            if (string.IsNullOrEmpty(templateId)) { return null; }

            if (_types.TryGetValue(templateId, out Type? templateType))
            {
                var template = (Template)Activator.CreateInstance(templateType)!;

                if (_outputCallbacks.TryGetValue(templateId, out OutputCallback? outputCallback))
                {
                    return (template, outputCallback);
                }

                return (template, null);
            }
            return null;
        }

        private (T, OutputCallback?)? Retrieve<T>() where T : Template, new()
        {
            Type type = typeof(T);

            if (string.IsNullOrEmpty(type.FullName)) { return null;}

            return ((T, OutputCallback?)?)Retrieve(type.FullName);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _types.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _types.Keys.GetEnumerator();
        }
    }
}
