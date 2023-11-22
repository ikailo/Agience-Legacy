import asyncio
from paho.mqtt import client as mqtt_client
from .broker_message import BrokerMessage
from .constants import BROKER_URI
from .identity import Identity

PORT = 8083

class MqttClient:
    def __init__(self, identity: Identity, mqtt_message_received_callback):
        self.identity = identity
        self.mqtt_message_received_callback = mqtt_message_received_callback
        self.client = mqtt_client.Client(protocol=mqtt_client.MQTTv5)

        self.client.tls_set()
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message
        # self.client.username_pw_set(identity.tokens[identity.authority.broker_uri], "password")
        # self.client.username_pw_set(identity.tokens[BROKER_URI], "password")

    async def connect_async(self, broker_uri):
        self.client.username_pw_set(self.identity.tokens[broker_uri], "password")
        self.client.connect_async(broker_uri, PORT)
        self.client.loop_start()

    def disconnect_async(self):
        self.client.loop_stop()
        self.client.disconnect()

    def on_connect(self, client, userdata, flags, rc):
        if rc == 0:
            print("Connected to MQTT Broker!")
        else:
            print("Failed to connect, return code %d\n", rc)

    def on_message(self, client, userdata, msg):
        # args = BrokerMessage.from_mqtt_args(msg)
        broker_message = BrokerMessage.from_mqtt_args(msg)
        asyncio.run(self.mqtt_message_received_callback(broker_message))

    async def subscribe_async(self, subscribe_mask):
        self.client.subscribe(subscribe_mask)

    async def publish_async(self, topic, payload, message_type):
        self.client.publish(topic, payload=payload, properties={'user_property': [(BrokerMessage.MESSAGE_TYPE, message_type)]})
