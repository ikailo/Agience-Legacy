using Agience.Client.MQTT.Model;
using MQTTnet.Client;
using System.Collections.Concurrent;

namespace Agience.Client.MQTT
{
    public class Broker
    {
        private string _brokerUri;        

        private readonly MqttClient _mqtt = new();
        //private readonly Identity _identity;

        //private readonly ConcurrentDictionary<string, Agent.OutputCallback> _outputCallbacks = new();

        /*

        Broker:
            - incoming messages -> (type, targetId)
            - outgoing messages -> (type, targetId)
            - understands topics
        
        Agency:
            - Timeline
            
        Agent:
            - Information
            - Template Processing
        
        Instance:
            - Catalog
            - Status

         */

        public Broker(string brokerUri)
        {
            _brokerUri = brokerUri;
        }

        /*
        public Broker(Identity identity) { 
            
            //_identity = identity;
            //_mqtt = new MqttClient();
            //_mqtt.MessageReceived += _mqtt_MessageReceived;
        }*/

        private void _mqtt_MessageReceived(object? sender, Message e)
        {
            throw new NotImplementedException();
        }

        /*
private async void _mqtt_MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs args)
{
   var message = Message.FromMqttArgs(args);

   switch (message.MessageType)
   {
       case AgentMessageType.STATUS:
           await Receive(message.MessageData as Status);
           break;
       case AgentMessageType.TEMPLATE:
           await Receive(message.MessageData as Template);
           break;
       case AgentMessageType.INFORMATION:
           await Receive(message.MessageData as Information);
           break;
   }
}
        */

        internal async Task Send(Status status, string toAgentId = "0")
        {
            await Logger.Write($"{toAgentId} {status.AgentId} status send");

            await Send(AgentMessageType.STATUS, status, toAgentId);
        }

        internal async Task Send(Template template, string toAgentId)
        {
            await Logger.Write($"{toAgentId} {template.Id} template send");

            await Send(AgentMessageType.TEMPLATE, template, toAgentId);
        }

        public async Task Send(Information information, string toAgentId)
        {            
            await Logger.Write($"{toAgentId} {information.Id} information send");

            await Send(AgentMessageType.INFORMATION, information, toAgentId);
        }

        public async Task Send(AgentMessageType messageType, object? messageData, string toAgentId = "0")
        {   
            var brokerMessage = new Message()
            {
                MessageType = messageType,
                MessageData = messageData,
                ToAgentId = toAgentId
            };

            string messageJson = brokerMessage.ConvertMessageDataToString();

            //await _mqtt.PublishAsync(brokerMessage.Topic, messageJson, brokerMessage.MessageType);
        }

        internal Task ConnectAsync(string token)
        {
            return _mqtt.ConnectAsync(_brokerUri, token);            
        }

        internal Task SubscribeAsync(string subscribeTopic)
        {
            throw new NotImplementedException();
        }

        internal Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }
    }
}
