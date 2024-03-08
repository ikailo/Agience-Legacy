using Agience.Model;
using Azure.Core;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using static QuikGraph.Algorithms.Assignment.HungarianAlgorithm;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace Agience.Client
{
    public class Agent
    {

        // TODO: Implement layer processing. Check link for more info.
        // https://github.com/daveshap/ACE_Framework/blob/main/publications/Conceptual%20Framework%20for%20Autonomous%20Cognitive%20Entities%20(ACE).pdf
        // https://github.com/daveshap/ACE_Framework/blob/main/ACE_PRIME/HelloAF/src/ace/resources/core/hello_layers/prompts/templates/ace_context.md

        // TODO: Agents should operate in a defined layer.

        private const int JOIN_WAIT = 5000;
        public string? Id { get; internal set; }
        public string? Name { get; internal set; }
        public bool IsConnected { get; private set; }
        public Agency Agency => _agency;
        public string Timestamp => _broker.Timestamp;
        //public History History { get; } = new(); // TODO: Make History ReadOnly for external access

        private ChatHistory _chatHistory;
        private PromptExecutionSettings? _promptExecutionSettings;
        private readonly ConcurrentDictionary<string, Runner> _informationCallbacks = new();
        private readonly Timer _representativeClaimTimer = new Timer(JOIN_WAIT);
        private readonly Authority _authority;
        private readonly Agency _agency;
        private readonly Broker _broker;


        private ILogger _logger => Kernel.LoggerFactory.CreateLogger<Agent>();

        public Kernel Kernel { get; internal set; }

        public async Task<IReadOnlyList<ChatMessageContent>> InvokeAsync(IReadOnlyList<ChatMessageContent> messages, CancellationToken cancellationToken = default)
        {   
            // TODO: Will need to summarize previous messages. This will get large.
            _chatHistory.AddRange(messages);

            var chatCompletionService = this.Kernel.GetRequiredService<IChatCompletionService>();

            var chatMessageContent = await chatCompletionService.GetChatMessageContentsAsync(
                _chatHistory,
                _promptExecutionSettings,
                this.Kernel,
                cancellationToken).ConfigureAwait(false);

            return chatMessageContent;
        }


        internal Agent(Authority authority, Broker broker, Model.Agency modelAgency, string? persona)
        {
            _authority = authority;
            _broker = broker;
            _agency = new Agency(authority, this, broker)
            {
                Id = modelAgency.Id,
                Name = modelAgency.Name
            };
            _chatHistory = new ChatHistory(persona ?? string.Empty);

            _promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            _representativeClaimTimer.AutoReset = false;
            _representativeClaimTimer.Elapsed += (s, e) => SendRepresentativeClaim();
        }

        internal async Task Connect()
        {
            if (!IsConnected)
            {
                await _broker.Subscribe(_authority.AgentTopic("+", Id!), _broker_ReceiveMessage);
                await _agency.Connect();
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
                await _broker.Unsubscribe(_authority.AgentTopic("+", Id!));
                // TODO: Need to let the Agency know our templates are no longer available
                await _agency.Disconnect();
                IsConnected = false;
            }
        }

        private void SendJoin()
        {
            _logger.LogDebug("SendJoin");

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "join" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(this.ToAgienceModel()) },
                    { "random", new Random().NextInt64().ToString() }
                }
            });
        }

        private void SendRepresentativeClaim()
        {
            if (_agency.RepresentativeId != null) { return; } // Was set by another agent

            _logger.LogDebug("SendRepresentativeClaim");

            _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AgencyTopic(Id!, _agency.Id!),
                Data = new Data
                {
                    { "type", "representative_claim" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(this.ToAgienceModel()) },
                }
            });
        }

        internal void SendInformationToAgent(Information information, string targetAgentId, Runner? runner = null)
        {
            _logger.LogDebug("SendInformationToAgent");

            if (runner != null)
            {
                _informationCallbacks[information.Id!] = runner;
            }

            _broker.Publish(new Message()
            {
                Type = MessageType.INFORMATION,
                Topic = _authority.AgentTopic(Id!, targetAgentId),
                Information = information
            });
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || (message.Data == null && message.Information == null)) { return; }

            // Incoming Agency Welcome message
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "welcome" &&
                message.Data?["agency"] != null &&
                message.Data?["representative_id"] != null &&
                message.Data?["timestamp"] != null &&
                message.Data?["agents"] != null)// &&
                                                //message.Data?["templates"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agency = JsonSerializer.Deserialize<Model.Agency>(message.Data?["agency"]!);
                var representativeId = message.Data?["representative_id"]!;
                var agents = JsonSerializer.Deserialize<List<Model.Agent>>(message.Data?["agents"]!);
                var agentTimestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(message.Data?["agent_timestamps"]!);

                if (agency?.Id == message.SenderId && agency.Id == _agency.Id && agents != null && agentTimestamps != null)
                {
                    _agency.ReceiveWelcome(agency, representativeId, agents, agentTimestamps, (DateTime)timestamp);
                }
            }

            // Incoming Agent Information message
            if (message.Type == MessageType.INFORMATION &&
                message.Information != null)
            {
                await ReceiveInformation(message.Information);
            }
        }

        private async Task ReceiveInformation(Information information)
        {
            _logger.LogInformation($"ReceiveInformation {information.Id}");

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

        internal Model.Agent ToAgienceModel()
        {
            return new Model.Agent()
            {
                Id = Id,
                Name = Name
            };
        }
    }
}