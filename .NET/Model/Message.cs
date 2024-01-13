using Agience;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using System.Text.Json;

namespace Agience.Client.MQTT.Model
{
    public enum AgentMessageType
    {
        STATUS,
        TEMPLATE,
        INFORMATION,
        //CONTEXT
    }

    public class Message //: Agience.Model.Message
    {
        internal const string MESSAGE_TYPE = "messagetype";
        private const string TOPIC_DELIMITER = "/";

        public string? AgencyId { get; set; }
        public string? ToAgentId { get; set; }
        public string Topic { get { return $"{AgencyId ?? "-"}TOPIC_DELIMITER{ToAgentId ?? "-"}"; } }
        public AgentMessageType MessageType { get; set; }
        public object? MessageData { get; set; }

        /*
        private Message() { }
        
        internal Message(Identity identity)
        {
            
        }
        /*
        internal static Message FromMqttArgs(MqttApplicationMessageReceivedEventArgs args)
        {
            var topicParts = args.ApplicationMessage.Topic.Split(TOPIC_DELIMITER);

            var brokerMessage = new Message()
            {
                AuthorityId = topicParts[0],
                UserId = topicParts[1],
                AgencyId = topicParts[2],
                AgentId = topicParts[3],
                InstanceId = topicParts[4],
                ModuleId = topicParts[5]
            };

            var payload = args.ApplicationMessage.ConvertPayloadToString();

            foreach (MqttUserProperty property in args.ApplicationMessage.UserProperties)
            {
                if (property.Name == MESSAGE_TYPE)
                {
                    switch (property.Value)
                    {
                        case "STATUS":
                            brokerMessage.MessageType = AgentMessageType.STATUS;
                            brokerMessage.MessageData = JsonSerializer.Deserialize<Status>(payload);
                            break;
                        case "TEMPLATE":
                            brokerMessage.MessageType = AgentMessageType.TEMPLATE;
                            brokerMessage.MessageData = JsonSerializer.Deserialize<Template>(payload);
                            break;
                        case "INFORMATION":
                            brokerMessage.MessageType = AgentMessageType.INFORMATION;
                            brokerMessage.MessageData = JsonSerializer.Deserialize<Information>(payload);
                            break;
                    }
                    break;
                }
            }
            return brokerMessage;
        }
        */

        internal string ConvertMessageDataToString()
        {
            switch (MessageType)
            {
                case AgentMessageType.STATUS:
                    return JsonSerializer.Serialize(MessageData as Status);
                case AgentMessageType.TEMPLATE:
                    return JsonSerializer.Serialize(MessageData as Template);
                case AgentMessageType.INFORMATION:
                    return JsonSerializer.Serialize(MessageData as Information);
                default:
                    throw new InvalidDataException($"Unknown message type: {MessageType}");
            }
        }
    }
}
