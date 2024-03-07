using Agience.Client;
using Agience.Agents._Console.Plugins;
using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Plugins.Grpc;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Agience.Agents_Console.Plugins;
using Microsoft.Extensions.Logging;

namespace Agience.Agents._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();

        private static Host? _host;

        private static Agent? _contextAgent;

        private static ILogger<Program>? _logger; // TODO: Add logging

        internal static async Task Main(string[] args)
        {
            if (string.IsNullOrEmpty(_config.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
            if (string.IsNullOrEmpty(_config.ClientId)) { throw new ArgumentNullException("ClientId"); }
            if (string.IsNullOrEmpty(_config.ClientSecret)) { throw new ArgumentNullException("ClientSecret"); }

            var builder = new HostBuilder()
            .WithAuthorityUri(_config.AuthorityUri)
            .WithCredentials(_config.ClientId, _config.ClientSecret)
            .WithBrokerUriOverride(_config.BrokerUriOverride)

            // Add local plugins to the host. Local plugins can be invoked by local or remote agents, if they are exposed (TODO).
            .AddPluginFromType<ConsolePlugin>()

            // Add local services to the host. Local services can be invoked by local agents only. 
            .AddService(ServiceDescriptor.Singleton<IConsoleService, ConsoleService>());
            
            _host = builder.Build();

            _host.AgentConnected += _host_AgentConnected;
            _host.AgentReady += _host_AgentReady;

            await _host.Run();
            
            // TODO: Add remote plugins/functions to the host (MQTT, GRPC, HTTP) that we want the local Kernels to consider local.
            // TODO: Probably this should be done in the Functions themselves, so it can be dynamic and lazy initialized.
            // TODO: Initiate plugin imports from Authority.
            // _host.ImportPluginFromGrpcFile("path-to.proto", "plugin-name");

            // TODO: Expose local plugins to remote via MQTT, GRPC, HTTP.
        }

        private static Task _host_AgentConnected(Agent agent) {

            // Agent instantiation is initiated from Authority-Manage.The Host does not have control.
            // Returns an agent that has access to all the local & psuedo-local functions
            // Agent has an Agency which connects them directly to other agents who are experts in their domain.

            Console.WriteLine($"{agent.Name} Connected");

            return Task.CompletedTask;
        }

        private static async Task _host_AgentReady(Agent agent)
        {
            Console.WriteLine($"{agent.Name} Ready");

            if (_contextAgent == null)
            {
                SetAgentContext(agent.Id);
                await RunConsole();
            }
        }

        // TODO: Read the input and set the context agent. For now, we will just use the first agent.
        // TODO: Callbacks from functions

        private static void SetAgentContext(string? agentId)
        {
         _contextAgent = _host!.GetAgent(agentId);
         Console.WriteLine($"Switched to {_contextAgent.Name} > ");
        }

        private static async Task RunConsole() {
          
            if (_contextAgent == null) { throw new InvalidOperationException("No context agent"); }

            // Here we want to communicate with the context agent.


            await _contextAgent.InvokeAsync("prompt");

            // BELOW FOR REFERENCE

            /// Create chat history
            var history = new ChatHistory();

            // Get chat completion service
            var chatCompletionService = _contextAgent.Kernel.GetRequiredService<IChatCompletionService>();

            Console.Write("User > ");

            string? userInput;

            while ((userInput = Console.ReadLine()) != null)
            {
                // Add user input
                history.AddUserMessage(userInput);

                // Enable auto function calling
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                // Get the response from the AI
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _contextAgent.Kernel);

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);

                // Get user input again
                Console.Write($"User > ");
            }

            /*
            // ====== Option 2 ======

            Data? message = "User > ";

            while (_host!.IsConnected)
            {
                var response = await _contextAgent.Kernel.InvokePromptAsync((string?)message ?? string.Empty);

                message = response.GetValue<Data?>();
            }*/

        }
    }
}