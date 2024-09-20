﻿using Microsoft.Extensions.Logging;

namespace Agience.SDK.Logging
{
    public class AgienceEventLogArgs : EventArgs
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public object? State { get; set; }
        public Exception? Exception { get; set; }
        public Func<object, Exception?, string>? Formatter { get; set; }
        public string? AgencyId { get; set; }
        public string? AgentId { get; set; }
    }
}