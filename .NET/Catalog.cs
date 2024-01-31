
namespace Agience.Client
{
    public class Catalog
    {
        private Dictionary<string, Type> _factories = new();
        private Dictionary<string, Func<Agent, Data?, Task>> _callbacks = new();

        public void Add<T>(Func<Agent, Data?, Task>? callback = null) where T : Template, new()
        {
            Type type = typeof(T);

            if (string.IsNullOrEmpty(type.FullName))
            {
                throw new ArgumentNullException(nameof(type.FullName));
            }

            _factories[type.FullName] = typeof(T);

            if (callback != null)
            {
                _callbacks[typeof(T).FullName!] = callback;
            }
        }

        internal (T, Func<Agent, Data?, Task>?)? Retrieve<T>() where T : Template, new()
        {
            Type type = typeof(T);

            if (string.IsNullOrEmpty(type.FullName))
            {
                throw new ArgumentNullException(nameof(type.FullName));
            }

            if (_factories.TryGetValue(type.FullName!, out Type? templateType))
            {
                var template = new T();

                if (_callbacks.TryGetValue(type.FullName, out Func<Agent, Data?, Task>? callback))
                {
                    return (template, callback);
                }

                return (template, null);
            }

            return null;
        }
    }
}
