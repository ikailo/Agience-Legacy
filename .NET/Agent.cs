using System.Collections.Concurrent;
using Timer = System.Timers.Timer;

namespace Agience.Client
{
    public class Agent : Model.Agent
    {   
        public new Instance? Instance { get; internal set; }
        public new Agency? Agency { get; internal set; }
        public bool IsConnected { get; internal set; }

        private Authority _authority;

        public Agent(Authority authority)
        {
            _authority = authority;
        }

        internal async Task Connect(Broker broker)
        {   
            await broker.SubscribeAsync($"+/{_authority.Id}/-/-/{Id}", ReceiveMessageCallback);

            if (Agency != null && !Agency.IsConnected)
            {
                await Agency.Connect(broker);
            }

            IsConnected = true;
        }

        private async Task ReceiveMessageCallback(Message message)
        {
            throw new NotImplementedException();
        }

        public Task<Data?> Prompt(Data data)
        {
            throw new NotImplementedException();
        }

        public Task Prompt(string v1, string v2)
        {
            throw new NotImplementedException();
        }

        public Task<Data?> Prompt(Data userInput, object value, string v)
        {
            throw new NotImplementedException();
        }
    }
}