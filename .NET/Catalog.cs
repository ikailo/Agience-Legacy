
namespace Agience.Client
{
    public class Catalog
    {
        private Dictionary<string, Type> _factories = new();
        private Dictionary<string, Func<Agent, Data?, Task<Data?>>?> _callbacks = new();

        public void Add<T>(Func<Agent, Data?, Task<Data?>>? callback = null) where T : Template
        {
            if (string.IsNullOrEmpty(typeof(T).FullName))
            {
                throw new ArgumentNullException(nameof(Type.FullName));
            }

            var constructor = typeof(T).GetConstructor(new[] { typeof(Agent) }); // TODO: this will fail if T is Template and not derived.
            if (constructor == null)
            {
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not have a constructor that takes an Agent parameter.");
            }

            _factories[typeof(T).FullName!] = typeof(T);

            if (callback != null)
            {
                _callbacks[typeof(T).FullName!] = callback;
            }
        }

        public T? Retrieve<T>(Agent agent) where T : Template
        {
            if (string.IsNullOrEmpty(typeof(T).FullName))
            {
                throw new ArgumentNullException(nameof(Type.FullName));
            }

            if (_factories.TryGetValue(typeof(T).FullName!, out Type? templateType))
            {
                var constructor = typeof(T).GetConstructor(new[] { typeof(Agent) }); // TODO: this will fail if T is Template and not derived.
                if (constructor == null)
                {
                    throw new InvalidOperationException($"Type {templateType.FullName} does not have a constructor that takes an Agent parameter.");
                }

                T template = (T)constructor.Invoke(new object[] { agent });

                if (_callbacks.TryGetValue(typeof(T).FullName!, out Func<Agent, Data?, Task<Data?>>? callback))
                {
                    template.OnCallback += callback;
                }

                return template;
            }

            return null;
        }
    }
}
