using Agience.Client;
using Agience.Agents._Console.Plugins;
using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Plugins.Grpc;
using System;
using Agience.Client.Templates.Default;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MQTTnet.Server;
using Microsoft.Extensions.DependencyInjection;
using Agience.Agents_Console.Plugins;

namespace Agience.Agents._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();

        private static Host? _host;

        internal static async Task Main(string[] args)
        {
            if (string.IsNullOrEmpty(_config.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
            if (string.IsNullOrEmpty(_config.ClientId)) { throw new ArgumentNullException("ClientId"); }
            if (string.IsNullOrEmpty(_config.ClientSecret)) { throw new ArgumentNullException("ClientSecret"); }

            var builder = new HostBuilder()
            .WithAuthorityUri(_config.AuthorityUri)
            .WithCredentials(_config.ClientId, _config.ClientSecret)
            .WithBrokerUriOverride(_config.BrokerUriOverride)
            .AddPluginFromType<ConsolePlugin>()
            .AddService(ServiceDescriptor.Singleton<IConsoleService, ConsoleService>());

            _host = builder.Build();

            _host.AgentConnected += _host_AgentConnected;
            _host.AgentReady += _host_AgentReady;

            await _host.Run();

            // The host will call back to the service to invoke methods I provide.

            // TODO: Add remote plugins/functions to the host (MQTT, GRPC, HTTP) that we want the local Kernels to consider local.
            // TODO: Probably this should be done in the Functions themselves, so it can be dynamic and lazy initialized.
            // TODO: Initiate plugin imports from Authority.           
            // _host.ImportPluginFromGrpcFile("path-to.proto", "plugin-name");

            // TODO: Add local services to the host. Local services can be invoked by local or remote agents. 
            // _host.AddSingleton("<OpenAIConnection>");
            // TODO: Add remote services to the host.
            // TODO: Expose local plugins to remote via MQTT, GRPC, HTTP.
        }

        private static Task _host_AgentConnected(Agent agent) { 

            Console.WriteLine($"{agent.Name} Connected");

            return Task.CompletedTask;
        }

        private static async Task _host_AgentReady(Agent agent)
        {
            Console.WriteLine($"{agent.Name} Ready");

            // Agent instantiation is initiated from Authority-Manage.The Host does not have control.
            // Returns an agent that has access to all the local & psuedo-local functions
            // Agent has an Agency which connects them directly to other agents who are experts in their domain.

            // Here we want to communicate with our local agent.

            // ====== Option 1 ======

            /// Create chat history
            var history = new ChatHistory();

            // Get chat completion service
            var chatCompletionService = agent.Kernel.GetRequiredService<IChatCompletionService>();            

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
                    kernel: agent.Kernel);

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);

                // Get user input again
                Console.Write("User > ");
            }


            // ====== Option 2 ======

            Data? message = "User > ";

            while (_host!.IsConnected)
            {
                var response = await agent.Kernel.InvokePromptAsync((string?)message ?? string.Empty);

                message = response.GetValue<Data?>();
            }

            Console.WriteLine($"Host Stopped");
        }

        private static async Task GetInputFromUser_callback(Agent agent, Data? output)
        {

            agent.Kernel.FunctionInvoking += Kernel_FunctionInvoking;
            if (((string?)output)?.StartsWith("echo:") ?? false)
            {
                //var response = await runner.Echo(((string?)output)?.Substring(5));

                //Console.WriteLine(response.Output);
            }

            if (((string?)output)?.StartsWith("log:") ?? false)
            {
                //runner.Log(((string?)output)?.Substring(4) ?? string.Empty);
            }

            if (((string?)output)?.StartsWith("web:") ?? false)
            {
                //await runner.DispatchAsync("Agience.Agents.Web.Templates.IncomingWebChatMessage", ((string?)output)?.Substring(4));
            }

            if (output == "quit")
            {
                Console.WriteLine("Stopping Host");
                await _host.Stop();
            }
        }

        private static void Kernel_FunctionInvoking(object? sender, FunctionInvokingEventArgs e)
        {
            e.
            throw new NotImplementedException();
        }
    }
}