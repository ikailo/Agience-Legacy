using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using System.Text.Json;

namespace Technologai
{
    public enum AgentMessageType
    {
        PULSE,
        TEMPLATE,
        INFORMATION,
        //CONTEXT
    }

    public class BrokerMessage
    {
        internal const string MESSAGE_TYPE = "messagetype";
        private const string TOPIC_DELIMITER = "/";

        public string? AgencyId { get; set; }
        public string? ToMemberId { get; set; }
        public string Topic { get { return $"{AgencyId ?? "-"}/{ToMemberId ?? "-"}"; } }
        public AgentMessageType MessageType { get; set; }
        public object? MessageData { get; set; }

        private BrokerMessage() { }

        internal BrokerMessage(Identity identity)
        {
            AgencyId = identity.AgencyId;
        }

        internal static BrokerMessage FromMqttArgs(MqttApplicationMessageReceivedEventArgs args)
        {
            var topicParts = args.ApplicationMessage.Topic.Split(TOPIC_DELIMITER);

            var brokerMessage = new BrokerMessage()
            {
                AgencyId = topicParts[0],
                ToMemberId = topicParts[1],
            };

            var payload = args.ApplicationMessage.ConvertPayloadToString();

            foreach (MqttUserProperty property in args.ApplicationMessage.UserProperties)
            {
                if (property.Name == MESSAGE_TYPE)
                {   
                    switch (property.Value)
                    {
                        case "PULSE":
                            brokerMessage.MessageType = AgentMessageType.PULSE;
                            brokerMessage.MessageData = JsonSerializer.Deserialize<Pulse>(payload);
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

        internal string ConvertMessageDataToString()
        {
            switch (MessageType)
            {
                case AgentMessageType.PULSE:
                    return JsonSerializer.Serialize(MessageData as Pulse);
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
