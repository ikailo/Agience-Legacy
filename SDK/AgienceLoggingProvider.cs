using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Agience.SDK
{

    public class AgienceLoggerProvider : ILoggerProvider
    {

        private readonly ConcurrentDictionary<string, AgienceLogger> _loggers = new();

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new AgienceLogger(name));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class AgienceLogger : ILogger
    {
        public Func<string, string, Task>? AgentLogEntryReceived { get; set; }
        public Func<string, string, Task>? AgencyLogEntryReceived { get; set; }

        private readonly string _categoryName;

        private readonly AsyncLocal<Dictionary<string, object>> _currentScope = new();

        public AgienceLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> stateDictionary)
            {

                var scope = _currentScope.Value;

                if (scope == null)
                {
                    scope = new Dictionary<string, object>();

                    _currentScope.Value = scope;
                }

                foreach (var item in stateDictionary)
                {
                    scope[item.Key] = item.Value;
                }

                return new DisposableScope(() =>
                {
                    foreach (var item in stateDictionary)
                    {
                        scope.Remove(item.Key);
                    }
                });
            }

            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var logMessage = formatter(state, exception);
            var scope = _currentScope.Value;

            string? agentId = null;

            if (scope != null && scope.TryGetValue("AgentId", out var agentIdObj))
            {
                agentId = agentIdObj as string;
            }

            if (!string.IsNullOrEmpty(agentId))
            {
                AgentLogEntryReceived?.Invoke(agentId, logMessage);
            }
            else
            {
                AgencyLogEntryReceived?.Invoke(_categoryName, logMessage);
            }

            // Console output
            Console.WriteLine($"{logLevel}: {_categoryName} - {logMessage} {(!string.IsNullOrEmpty(agentId) ? $"AgentId: {agentId}" : "")}");
        }

        private class DisposableScope : IDisposable
        {
            private readonly Action _disposeAction;

            public DisposableScope(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction();
            }
        }
    }
}