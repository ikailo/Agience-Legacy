using Microsoft.Extensions.Logging;

namespace Agience.SDK.Logging
{
    public class AgienceLogger<T> : ILogger<T>, IDisposable
    {
        private readonly ILogger<T> _innerLogger;
        private readonly IDisposable _scope;
        private readonly AsyncLocal<Scope?> _currentScope = new();

        public AgienceLogger(ILogger<T> innerLogger, string agentId)
        {
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
            _scope = _innerLogger.BeginScope(new Dictionary<string, object> { { "AgentId", agentId } }) ?? throw new InvalidOperationException("Failed to create scope.");
        }

        public Func<string, string, Task>? AgentLogEntryReceived { get; set; }
        public Func<string, string, Task>? AgencyLogEntryReceived { get; set; }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var scope = new Scope(this, state);
            return scope;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _innerLogger.IsEnabled(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var logMessage = formatter(state, exception);

            // Retrieve AgentId from the scope
            string? agentId = null;
            var scope = _currentScope.Value;
            while (scope != null)
            {
                if (scope.TryGetValue("AgentId", out var value))
                {
                    agentId = value as string;
                    break;
                }
                scope = scope.Parent;
            }

            // Log the message
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
            Console.WriteLine($"{logLevel}: {typeof(T).Name} - {logMessage} {(agentId != null ? $"AgentId: {agentId}" : "")}");
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        private class Scope : IDisposable
        {
            private readonly AgienceLogger<T> _logger;
            public Scope? Parent { get; }
            private readonly IDictionary<string, object> _state;

            public Scope(AgienceLogger<T> logger, object state)
            {
                _logger = logger;
                Parent = _logger._currentScope.Value;
                _state = state as IDictionary<string, object> ?? new Dictionary<string, object>();
                _logger._currentScope.Value = this;
            }

            public bool TryGetValue(string key, out object? value)
            {
                if (_state.TryGetValue(key, out value))
                {
                    return true;
                }
                return Parent?.TryGetValue(key, out value) ?? false;
            }

            public void Dispose()
            {
                _logger._currentScope.Value = Parent;
            }
        }
    }
}
