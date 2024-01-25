namespace Agience.Client.MQTT.Model
{
    public enum Status
    {
        ONLINE,
        OFFLINE
    }
    public class StatusMessage : Message
    {
        public DateTime CreatedTimestamp { get; }
        public string? SenderAddress { get;  }
        public Data Data { get; } = new();

        public StatusMessage(string senderAddress, Status status)
        {
            CreatedTimestamp = DateTime.UtcNow;
            SenderAddress = senderAddress;        
        }
    }
}
