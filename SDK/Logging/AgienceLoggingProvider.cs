// AgienceLoggerProvider.cs
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Agience.SDK.Logging
{
    public class AgienceLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
        private readonly IAgentIdProvider _agentIdProvider;

        public AgienceLoggerProvider(IAgentIdProvider agentIdProvider)
        {
            _agentIdProvider = agentIdProvider ?? throw new ArgumentNullException(nameof(agentIdProvider));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new AgienceLogger<ILogger>(new LoggerFactory().CreateLogger(name), _agentIdProvider.GetCurrentAgentId()));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
