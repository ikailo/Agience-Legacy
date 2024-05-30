using Agience.Hosts._Console.Plugins;
using Agience.SDK;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Agience.Hosts._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();

        private static SDK.Host? _host;

        private static SDK.Agent? _contextAgent;

        private static ILogger<Program>? _logger; // TODO: Add logging

        internal static async Task Main(string[] args)
        {
            if (string.IsNullOrEmpty(_config.HostName)) { throw new ArgumentNullException("HostName"); }
            if (string.IsNullOrEmpty(_config.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
            if (string.IsNullOrEmpty(_config.ClientId)) { throw new ArgumentNullException("ClientId"); }
            if (string.IsNullOrEmpty(_config.ClientSecret)) { throw new ArgumentNullException("ClientSecret"); }
            
            if (!string.IsNullOrEmpty(_config.CustomNtpHost) && !_config.CustomNtpHost.ToLower().EndsWith("pool.ntp.org"))
                throw new ArgumentException("The CustomNtpHost must end with `pool.ntp.org`."); 

            var builder = new HostBuilder()
            .WithName(_config.HostName)
            .WithAuthorityUri(_config.AuthorityUri)
            .WithCredentials(_config.ClientId, _config.ClientSecret)
            .WithBrokerUriOverride(_config.BrokerUriOverride)       
            .WithCustomNtpHost(_config.CustomNtpHost)   

            // TODO: I want to expose Functions and Agents to other Agents.
            // For example, we can publish two agents/plugins here: ConsolePlugin and EmailPlugin.

            // Add local plugins to the host. Local plugins can be invoked by local or remote agents, if they are exposed (TODO).
            // TODO: Add from a local assembly directory

            .AddPluginFromType<ConsolePlugin>()
            .AddPluginFromType<EmailPlugin>()
            .AddPluginFromType<AuthorEmailPlanner>()

            // TODO: Add Prompt Template Plugins

            // Add Chat Completion Service (OpenAI)
            .AddService(ServiceDescriptor.Singleton<IChatCompletionService>(
                serviceProvider => new OpenAIChatCompletionService("gpt-3.5-turbo", _config.OpenAiApiKey ?? throw new ArgumentNullException("OpenAiApiKey")))
            )

            // Add local services to the host. Local services can be invoked by local agents only. 
            .AddService(ServiceDescriptor.Singleton<IConsoleService>(new ConsoleService()));

            _host = builder.Build();

            _host.AgentBuilding += _host_AgentBuilding;
            _host.AgentConnected += _host_AgentConnected;
            _host.AgentReady += _host_AgentReady;            

            await _host.Run();

            // TODO: Add remote plugins/functions to the host (MQTT, GRPC, HTTP) that we want the local Kernels to consider local.
            // TODO: Probably this should be done in the Functions themselves, so it can be dynamic and lazy initialized.            
            // _host.ImportPluginFromGrpcFile("path-to.proto", "plugin-name");

            // TODO: Initiate plugin imports from Authority.

            // TODO: Expose local plugins to remote via MQTT, GRPC, HTTP.
        }

        private static Task _host_AgentBuilding(AgentBuilder builder)
        {
            builder.WithPersona(
                "You are a friendly assistant who likes to follow the rules. You will complete required steps " +
                "and request approval before taking any consequential actions. If the user doesn't provide " +
                "enough information for you to complete a task, you will keep asking questions until you have " +
                "enough information to complete the task."
                );

            return Task.CompletedTask;
        }

        private static Task _host_AgentConnected(Agent agent)
        {

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

        private static void SetAgentContext(string? agentId)
        {
            _contextAgent = _host!.GetAgent(agentId);
            Console.WriteLine($"* Switched context to {_contextAgent?.Name ?? "Unknown"} *");
        }

        private static async Task RunConsole()
        {

            if (_contextAgent == null) { throw new InvalidOperationException("No context agent"); }

            // Here we want to communicate with the context agent.

            /// Create chat history
            var chatHistory = new ChatHistory();

            Console.Write("User > ");

            string? userInput;

            while ((userInput = Console.ReadLine()) != null)
            {
                // Add user input
                chatHistory.AddUserMessage(userInput);

                var result = await _contextAgent.ProcessAsync(chatHistory);

                // Print the results
                foreach (var message in result)
                {
                    chatHistory.AddMessage(message.Role, message.Content ?? string.Empty);
                    Console.WriteLine($"{message.Role.ToString().Pascalize()} > {message.Content}");
                }

                // Get user input again
                Console.Write($"User > ");
            }
        }
    }
}