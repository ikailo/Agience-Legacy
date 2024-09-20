using Microsoft.Extensions.Logging;

namespace Agience.SDK.Logging
{
    // Helper class for composite logging to multiple loggers
    public class CompositeLoggerBase : ILogger
    {
        private readonly IEnumerable<ILogger> _loggers;

        public CompositeLoggerBase(IEnumerable<ILogger> loggers)
        {
            _loggers = loggers;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            // Forward BeginScope to all loggers
            foreach (var logger in _loggers)
            {
                logger.BeginScope(state);
            }
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Check if any logger is enabled for the given log level
            foreach (var logger in _loggers)
            {
                if (logger.IsEnabled(logLevel))
                {
                    return true;
                }
            }
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Log the message to all loggers
            foreach (var logger in _loggers)
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, state, exception, formatter);
                }
            }
        }
    }

    public class CompositeLogger<T> : CompositeLoggerBase, ILogger<T>
    {
        public CompositeLogger(List<ILogger> loggers)
            : base(loggers)
        {
        }
    }

    public class CompositeLogger : CompositeLoggerBase
    {
        public CompositeLogger(List<ILogger> loggers)
            : base(loggers)
        {
        }
    }
}
