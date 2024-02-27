using System.Text.Json;

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
        //public string? Payload { get; set; }

        private object? _content;

        public Data? Data
        {
            get => Type == MessageType.EVENT ? (Data?)_content : null;
            set
            {
                if (Type == MessageType.EVENT)
                {
                    _content = value;
                    //Payload = value?.Raw;
                }
            }
        }

        public Information? Information
        {
            get => Type == MessageType.INFORMATION ? (Information?)_content : null;
            set
            {
                if (Type == MessageType.INFORMATION)
                {
                    _content = value;
                    //Payload = value != null ? JsonSerializer.Serialize(value) : null;
                }
            }
        }
    }
}