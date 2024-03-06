using Agience.Client;
using Agience.Agents._Console.Plugins;
using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Plugins.Grpc;
using System;
using Agience.Client.Templates.Default;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Agience.Agents._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();

        private static Host? _host;

        internal static async Task Main(string[] args)
        {

            // TODO: Host Builder

            // HERE

            _host = new Host(_config);

            // ===PLUGINS===

            // Add local plugins/functions to the host. Local plugins contain functions that can be invoked by local or remote agents.
            _host.ImportPluginFromType<ConsolePlugin>();

            // TODO: Add remote plugins/functions to host (MQTT, GRPC, HTTP) that we want the local Kernels to consider local.
            // _host.ImportPluginFromGrpcFile("path-to.proto", "plugin-name");

            // TODO: How to handle callbacks?  PluginFactory? FunctionFactory?
            // _host.AddTemplate<GetInputFromUser>(GetInputFromUser_callback);

            // ===SERVICES===

            // TODO: Add local services to the host. Local services can be invoked by local or remote agents. 
            // _host.AddSingleton("<OpenAIConnection>");

            // TODO: Add remote services to the host.

            // TODO: Expose local plugins to remote via MQTT, GRPC, HTTP.

            // ===AGENTS===

            // Next, we create AgentPlugins definitions..
            // Agents have a defined set of functions and services. All functions and services must be local or psuedo local.
            // What is an agent?
            // An Agent is an expert in its specific information domain.
            // Agent accepts a single input prompt.
            // Agents have a specific configuration. Configurable from the Authority?

           _host.AddAgentBuilder(
               "Agience.Agents.Console",
               Agent.CreateBuilder()
               // .WithAnything() // Add some other configuration to the agent.
               //.WithService() // Add local services to the agent.
               );


            _host.AgentConnected += _host_AgentConnected;
            _host.AgentReady += _host_AgentReady;

            await _host.Run();
        }

        private static Task _host_AgentConnected(Agent agent)
        {
            Console.WriteLine($"{agent.Agency.Name} / {agent.Name} Connected");

            // Do we set default plugins here?
            // Do we still need default plugins?            
            // xx Update default templates here. TODO: only if allowed

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

        private static async Task GetInputFromUser_callback(Runner runner, Data? output)
        {
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
    }
}