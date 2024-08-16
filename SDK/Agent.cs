#pragma warning disable SKEXP0001

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;
using System.Text.Json;
using Timer = System.Timers.Timer;
using AutoMapper;
using Agience.SDK.Mappings;
using Agience.SDK.Models.Messages;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Entities.Agent), ReverseMap = true)]
    public class Agent
    {
        private const int JOIN_WAIT = 5000;
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public bool IsConnected { get; private set; }
        public Agency Agency => _agency;
        public Kernel Kernel => _kernel;
        public string Timestamp => _broker.Timestamp;

        //private readonly ChatHistory _chatHistory;
        private readonly ConcurrentDictionary<string, Runner> _informationCallbacks = new();
        private readonly Timer _representativeClaimTimer = new Timer(JOIN_WAIT);
        private readonly Authority _authority;
        private readonly Agency _agency;
        private readonly Broker _broker;
        private readonly ILogger _logger;
        private readonly Kernel _kernel;
        private readonly IMapper _mapper;

        //private PromptExecutionSettings? _promptExecutionSettings;
        private string _persona;

        internal Agent(
            string id,
            string name,
            Authority authority,
            Broker broker,
            Agency agency,
            string? persona,
            Kernel kernel,
            ILogger<Agent> logger
            )

        {
            Id = id;
            Name = name;

            _authority = authority;
            _broker = broker;
            _agency = agency;
            _persona = persona ?? string.Empty; // TODO: Get Agent's persona
            _kernel = kernel;
            _logger = logger;

            _mapper = AutoMapperConfig.GetMapper();
            //_chatHistory = new();

            //_promptExecutionSettings = new OpenAIPromptExecutionSettings
            //{
            //    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            //};

            _representativeClaimTimer.AutoReset = false;
            _representativeClaimTimer.Elapsed += (s, e) => SendRepresentativeClaim();
        }

        internal async Task Connect()
        {
            if (!IsConnected)
            {
                await _broker.Subscribe(_authority.AgentTopic("+", Id!), _broker_ReceiveMessage);
                IsConnected = true;
            }
            SendJoin();
            _representativeClaimTimer.Start();
        }

        internal async Task Disconnect()
        {
            // TODO: Auto-disconnect via MQTT Will message

            if (IsConnected)
            {
                // TODO: Advise the Agency that this Agent is no longer available.

                await _broker.Unsubscribe(_authority.AgentTopic("+", Id!));
                IsConnected = false;
            }
        }

        private void SendJoin()
        {
            _logger?.LogDebug("SendJoin");

            _broker.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "join" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Entities.Agent>(this)) },
                    { "random", new Random().NextInt64().ToString() }
                }
            });
        }

        private void SendRepresentativeClaim()
        {
            if (_agency.RepresentativeId != null) { return; } // Was set by another agent

            _logger?.LogDebug("SendRepresentativeClaim");

            _broker.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "representative_claim" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Entities.Agent>(this)) },
                }
            });
        }

        /*
        internal void SendInformationToAgent(Information information, string targetAgentId, Runner? runner = null)
        {
            _logger?.LogDebug("SendInformationToAgent");

            if (runner != null)
            {
                _informationCallbacks[information.Id!] = runner;
            }

            _broker.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.INFORMATION,
                Topic = _authority.AgentTopic(Id!, targetAgentId),
                Information = information
            });
        }*/

        private async Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null || (message.Data == null && message.Information == null)) { return; }
            /*
            // Incoming Agency Welcome message
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "welcome" &&
                message.Data?["agency"] != null &&
                message.Data?["representative_id"] != null &&
                message.Data?["timestamp"] != null &&
                message.Data?["agents"] != null)// &&
                                                //message.Data?["templates"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agency = JsonSerializer.Deserialize<Models.Entities.Agency>(message.Data?["agency"]!);
                var representativeId = message.Data?["representative_id"]!;
                var agents = JsonSerializer.Deserialize<List<Models.Entities.Agent>>(message.Data?["agents"]!);
                var agentTimestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(message.Data?["agent_timestamps"]!);

                if (agency?.Id == message.SenderId && agency.Id == _agency.Id && agents != null && agentTimestamps != null)
                {
                    _agency.ReceiveWelcome(agency, representativeId, agents, agentTimestamps, (DateTime)timestamp);
                }
            }
            
            // Incoming Agent Information message
            if (message.Type == BrokerMessageType.INFORMATION &&
                message.Information != null)
            {
                await ReceiveInformation(message.Information);
            }*/
        }

        internal void AutoStart()
        {
            throw new NotImplementedException();
        }

        internal void Prompt()
        {

        }

        internal void ReceiveFromAgency()
        {
            throw new NotImplementedException();
        }


        /*
private async Task ReceiveInformation(Information information)
{
   _logger?.LogInformation($"ReceiveInformation {information.Id}");

   if (information.InputAgentId == Id)
   {

       // This is returned information
       if (_informationCallbacks.TryRemove(information.Id!, out Runner? runner))
       {
           //runner.ReceiveOutput(information);
           throw new NotImplementedException();
       }
   }

   if (information.OutputAgentId == null)
   {
       // This is information that needs to be processed. Presumably Local. Dispatch it.
       //await new Runner(this, information).DispatchAsync();
       throw new NotImplementedException();

       // Return the output to the input agent
       SendInformationToAgent(information, information?.InputAgentId!);
   }
}
        */

        public async Task<string> PromptAsync(string message, CancellationToken cancellationToken = default)
        {
            var chatHistory = new ChatHistory();

            chatHistory.AddUserMessage(message);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            var chatMessageContent = await chatCompletionService.GetChatMessageContentAsync(chatHistory, null, _kernel, cancellationToken);

            if (chatMessageContent != null)
            {
                // TODO: Are we certain that the response is always the last item?  Could there be multiple responses?
                var mimeType = chatMessageContent.Items.Last().MimeType;

                if (mimeType == "text/plain")
                {
                    return (string)(chatMessageContent.Items.Last().InnerContent ?? string.Empty);
                }
                else
                {
                    throw new NotImplementedException("unsupported chat message content type");
                }
            }

            return string.Empty;
        }
    }
}