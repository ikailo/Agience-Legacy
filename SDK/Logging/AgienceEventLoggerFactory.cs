using Agience.SDK.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Agience.SDK.Logging
{
    public class AgienceEventLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _innerFactory;
        private List<ILoggerProvider> _providers = new();

        public AgienceEventLoggerFactory(ILoggerFactory innerFactory, AgienceEventLoggerProvider provider)
        {
            _innerFactory = innerFactory;
            _providers.Add(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            var loggers = new List<ILogger>();
            foreach (var provider in _providers)
            {
                if (provider is AgienceEventLoggerProvider agienceEventLoggerProvider)
                {
                    loggers.Add(agienceEventLoggerProvider.CreateLogger(categoryName, "foo", "bar"));
                }
                else
                {
                    // TODO: Create a scope to store the agencyId and agentId
                    loggers.Add(provider.CreateLogger(categoryName));
                }
            }

            return new CompositeLogger(loggers);
        }
        /*
        public ILogger CreateLogger(string categoryName, string agencyId, string? agentId)
        {
            var loggers = new List<ILogger>();
            foreach (var provider in _providers)
            {   
                if (provider is AgienceEventLoggerProvider agienceEventLoggerProvider)
                {
                    loggers.Add(agienceEventLoggerProvider.CreateLogger(categoryName, agencyId, agentId));
                }
                else
                {
                    // TODO: Create a scope to store the agencyId and agentId
                    loggers.Add(provider.CreateLogger(categoryName));
                }
            }

            return new CompositeLogger(loggers);
        }*/

        public ILogger<T> CreateLogger<T>(string agencyId, string? agentId)
        {
            var loggers = new List<ILogger>();
            foreach (var provider in _providers)
            {
                
                if (provider is AgienceEventLoggerProvider agienceEventLoggerProvider)
                {
                    loggers.Add(agienceEventLoggerProvider.CreateLogger<T>(agencyId, agentId));
                }
                else
                {
                    // TODO: Create a scope to store the agencyId and agentId
                    loggers.Add(provider.CreateLogger(typeof(T).FullName ?? typeof(T).Name));
                }
            }

            return new CompositeLogger<T>(loggers);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _providers.Add(provider);
        }

        public void Dispose()
        {
            foreach (var provider in _providers)
            {
                provider.Dispose();
            }
            _providers.Clear();

            _innerFactory?.Dispose();
        }
    }
}
