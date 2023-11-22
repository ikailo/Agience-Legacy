import json
from enum import Enum

class AgentMessageType(Enum):
    PULSE = "PULSE"
    TEMPLATE = "TEMPLATE"
    INFORMATION = "INFORMATION"

class BrokerMessage:
    MESSAGE_TYPE = "messagetype"
    TOPIC_DELIMITER = "/"

    def __init__(self, identity=None, message_data=None, to_member_id=None, message_type=None):
        self.agency_id = identity.agency_id if identity else None
        self.to_member_id = to_member_id
        self.message_type = message_type
        self.message_data = json.dumps(message_data, default=lambda o: o.__dict__) if message_data else None

    @property
    def topic(self):
        return f"{self.agency_id or '-'}{self.TOPIC_DELIMITER}{self.to_member_id or '-'}"

    @classmethod
    def from_mqtt_args(cls, args):
        topic_parts = args.topic.split(cls.TOPIC_DELIMITER)
        broker_message = cls()
        broker_message.agency_id = topic_parts[0]
        broker_message.to_member_id = topic_parts[1]

        payload = args.payload.decode()

        for property in args.user_properties:
            if property['name'] == cls.MESSAGE_TYPE:
                broker_message.message_type = AgentMessageType(property['value'])
                broker_message.message_data = json.loads(payload)
                break

        return broker_message

    # def convert_message_data_to_string(self):
    #     if self.message_type == AgentMessageType.PULSE:
    #         return json.dumps(self.message_data, default=lambda o: o.__dict__)
    #     elif self.message_type == AgentMessageType.TEMPLATE:
    #         return json.dumps(self.message_data, default=lambda o: o.__dict__)
    #     elif self.message_type == AgentMessageType.INFORMATION:
    #         return json.dumps(self.message_data, default=lambda o: o.__dict__)
    #     else:
    #         raise ValueError(f"Unknown message type: {self.message_type}")