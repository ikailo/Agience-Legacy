namespace Agience.Client.MQTT.Model
{
    enum StatusType
    {
        ONLINE,
        OFFLINE,

    }

    public class Status //: Agience.Model.Status
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        //public DateTime NextStatusTime { get; set; } // TODO: Send on schedule
        public string? AgentId { get; set; }
        public Status(string agentId)
        {
            AgentId = agentId;
            //NextStatusTime = DateTime.UtcNow.AddSeconds(intervalSeconds);
        }
    }
}
