using Agience.SDK.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Agience.SDK.Logging
{
    public class AgienceEventLoggerProvider : ILoggerProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<AgienceEventLogArgs>? LogEntryReceived;

        public AgienceEventLoggerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName, string agencyId, string? agentId)
        {
            var logger = new AgienceEventLogger(agencyId, (typeof(Agency).FullName ?? typeof(Agency).Name) == categoryName ? null : agentId);

            var handler = _serviceProvider.GetService<IAgienceEventLogHandler>();

            logger.LogEntryReceived += (sender, e) => handler?.OnLogEntryReceived(sender, e);

            return logger;
        }

        public ILogger<T> CreateLogger<T>(string agencyId, string? agentId)
        {
            var logger = new AgienceEventLogger<T>(agencyId, typeof(T) == typeof(Agency) ? null : agentId);

            var handler = _serviceProvider.GetService<IAgienceEventLogHandler>();

            logger.LogEntryReceived += (sender, e) => handler?.OnLogEntryReceived(sender, e);

            return logger;
        }

        public void Dispose() { }
    }



    /*
    public class AgienceLoggerProvider : ILoggerProvider
    {   
        private readonly ConcurrentDictionary<string, AgienceLogger<Agent>> _agentLoggers = new();
        private readonly ConcurrentDictionary<string, AgienceLogger<Agency>> _agencyLoggers = new();

        public event Func<AgienceLogEventArgs, Task>? LogEntryReceived;

        public AgienceLoggerProvider()
        {
        }

        public ILogger<T> CreateLogger<T>(AgienceLoggerFactory loggerFactory)
        {
            if (typeof(T) == typeof(Agency))
            {
                var agencyLogger = loggerFactory.CreateLogger<Agency>();
                agencyLogger.LogEntryReceived += async (args) => await (LogEntryReceived?.Invoke(args) ?? Task.CompletedTask);
                return (ILogger<T>)new AgienceLogger<Agency>(agencyLogger, loggerFactory.AgencyId);
            }
            else if (typeof(T) == typeof(Agent))
            {
                var agentLogger = loggerFactory.CreateLogger<Agent>();
                agentLogger.LogEntryReceived += async (args) => await (LogEntryReceived?.Invoke(args) ?? Task.CompletedTask);
                return (ILogger<T>)new AgienceLogger<Agent>(agentLogger, loggerFactory.AgencyId, loggerFactory.AgentId);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported Type for AgienceLogger: {typeof(T).FullName ?? typeof(T).Name}");
            }
        }

        public void Dispose()
        {
            _agentLoggers.Values.ToList().ForEach(l => l.Dispose());
            _agencyLoggers.Values.ToList().ForEach(l => l.Dispose());
        }

        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        internal async Task OnLogEntryReceived(AgienceLogEventArgs args)
        {
            await (LogEntryReceived?.Invoke(args) ?? Task.CompletedTask);
        }
}*/
}
