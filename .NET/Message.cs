namespace Agience.Client
{
    public enum MessageType
    {
        EVENT,
        INFORMATION,
        CONTEXT,
        UNKNOWN
    }

    public class Message
    {
        public MessageType Type { get; set; } = MessageType.UNKNOWN;
        public string? Topic { get; set; } 
        public string? SenderId => Topic?.Split('/')[0];
        public string? Destination => Topic?.Substring(Topic.IndexOf('/') + 1);
        public Data? Payload { get; set; }
    }
}
