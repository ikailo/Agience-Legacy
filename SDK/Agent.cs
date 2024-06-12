using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;
using System.Text.Json;
using Timer = System.Timers.Timer;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Agience.SDK.Mappings;
using QuikGraph;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Agent), ReverseMap = true)]
    public class Agent
    {
        // TODO: Implement layer processing. Check link for more info.
        // https://github.com/daveshap/ACE_Framework/blob/main/publications/Conceptual%20Framework%20for%20Autonomous%20Cognitive%20Entities%20(ACE).pdf
        // https://github.com/daveshap/ACE_Framework/blob/main/ACE_PRIME/HelloAF/src/ace/resources/core/hello_layers/prompts/templates/ace_context.md

        // TODO: Agents should operate in a defined layer.

        private const int JOIN_WAIT = 5000;
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool IsConnected { get; private set; }
        public Kernel Kernel => _kernel;
        public Agency Agency { get; set; }
        public Host? Host { get; set; }
        public string Timestamp => _broker.Timestamp;

        //private readonly History _history = new(); // TODO: Make History ReadOnly for external access        

        private readonly ChatHistory _chatHistory;
        private readonly ConcurrentDictionary<string, Runner> _informationCallbacks = new();
        private readonly Timer _representativeClaimTimer = new Timer(JOIN_WAIT);
        private readonly Authority _authority;
        private readonly Agency _agency;
        private readonly Broker _broker;
        private readonly ILogger? _logger;
        private readonly Kernel _kernel;

        private readonly IMapper _mapper;

        private PromptExecutionSettings? _promptExecutionSettings;
        private string _persona;

        //public Agent() { }

        internal Agent(
            string? id,
            string? name,
            Authority authority,
            Broker broker,
            Models.Agency modelAgency,
            string? persona,
            IServiceProvider serviceProvider,
            KernelPluginCollection plugins)
        {
            _kernel = new Kernel(serviceProvider, plugins);

            _logger = Kernel.LoggerFactory.CreateLogger<Agent>();

            //TODO Part of the Architecture Review about the SDK and DI            
            //Kernel.LoggerFactory.AddProvider()

            _mapper = AutoMapperConfig.GetMapper();

            Id = id;
            Name = name;

            _authority = authority;
            _broker = broker;

            _agency = new Agency(authority, this, broker)
            {
                Id = modelAgency.Id,
                Name = modelAgency.Name
            };

            _persona = persona ?? string.Empty;

            _chatHistory = new();

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
                // TODO: Advise the Agency that this Agent is no longer available.

                await _broker.Unsubscribe(_authority.AgentTopic("+", Id!));
                await _agency.Disconnect();
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
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Agent>(this)) },
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
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Agent>(this)) },
                }
            });
        }

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
        }

        private async Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null || (message.Data == null && message.Information == null)) { return; }

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
                var agency = JsonSerializer.Deserialize<Models.Agency>(message.Data?["agency"]!);
                var representativeId = message.Data?["representative_id"]!;
                var agents = JsonSerializer.Deserialize<List<Models.Agent>>(message.Data?["agents"]!);
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
            }
        }

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

        public async IAsyncEnumerable<ChatMessage> ProcessAsync(string message)
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(message);

            var processResult = await ProcessAsync(chatHistory);

            foreach (var content in processResult)
            {
                // TODO: This can miss messages that have multiple items. This is a quick implementation for now.
                yield return new ChatMessage(content.Role.ToString(), content.Content ?? string.Empty);
            }
        }

        internal async Task<IReadOnlyList<ChatMessageContent>> ProcessAsync(IReadOnlyList<ChatMessageContent> messages, CancellationToken cancellationToken = default)
        {
            // TODO: Will need to summarize previous messages. This could get large.
            _chatHistory.AddRange(messages);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            var chatMessageContent = await chatCompletionService.GetChatMessageContentsAsync(
                _chatHistory,
                _promptExecutionSettings,
                _kernel,
                cancellationToken).ConfigureAwait(false);

            return chatMessageContent;
        }
    }
}