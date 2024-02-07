using Agience.Model;
using System.Collections;
using System.Reflection;

namespace Agience.Client
{
    public delegate Task OutputCallback(Agent agent, Data? data);

    internal class Catalog : IEnumerable<string>
    {
        private readonly Dictionary<string, Type> _types = new();
        private readonly Dictionary<string, OutputCallback> _globalCallbacks = new();

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
                _globalCallbacks[typeof(T).FullName!] = outputCallback;
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
            _globalCallbacks.Remove(type.FullName!);
        }

        internal (Template, OutputCallback?)? GetTemplateForAgent(string templateId, Agent agent)
        {
            (Template, OutputCallback?)? template = Retrieve(templateId, agent);

            return template.HasValue ? template.Value : null;

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

        private (Template, OutputCallback?)? Retrieve(string? templateId, Agent _agent)
        {
            if (string.IsNullOrEmpty(templateId)) { return null; }

            if (_types.TryGetValue(templateId, out Type? templateType))
            {
                var template = (Template?)Activator.CreateInstance(templateType);

                if (template != null)
                {
                    template.Agent = _agent;

                    if (_globalCallbacks.TryGetValue(templateId, out OutputCallback? globalCallback))
                    {
                        return (template, globalCallback);
                    }

                    return (template, null);
                }
            }
            return null;
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
