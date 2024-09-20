using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agience.SDK.Logging
{
    public class AgienceEventLoggerProvider : ILoggerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<ILogger> _createdLoggers = new();

        public AgienceEventLoggerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }

        public ILogger CreateLogger(string categoryName, string agencyId, string? agentId)
        {
            return CreateLoggerInternal(typeof(object), agencyId, agentId);
        }

        public ILogger<T> CreateLogger<T>(string agencyId, string? agentId)
        {
            return (ILogger<T>)CreateLoggerInternal(typeof(T), agencyId, agentId);            
        }

        private ILogger CreateLoggerInternal(Type loggerType, string agencyId, string? agentId)
        {
            ILogger logger = loggerType != typeof(object)
                ? (ILogger)(Activator.CreateInstance(typeof(AgienceEventLogger<>).MakeGenericType(loggerType), agencyId, agentId)
                    ?? throw new InvalidOperationException("Failed to create logger instance."))
                : new AgienceEventLogger(agencyId, agentId);

            foreach (var handler in _serviceProvider.GetServices<IAgienceEventLogHandler>())
            {
                if (logger is AgienceEventLogger agienceEventLogger)
                {
                    agienceEventLogger.LogEntryReceived += (sender, e) => handler.OnLogEntryReceived(sender, e);
                }
            }

            _createdLoggers.Add(logger);

            return logger;
        }

        public void Dispose()
        {
            foreach (var logger in _createdLoggers)
            {
                if (logger is IDisposable disposableLogger)
                {
                    disposableLogger.Dispose();
                }
            }
            _createdLoggers.Clear();
        }
    }
}