using Agience.Client;
using Agience.Agents._Console.Plugins;
using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Plugins.Grpc;
using System;

namespace Agience.Agents._Console
{
    internal class Program
    {
        private static readonly AppConfig _config = new();
        private static Host? _host;

        internal static async Task Main(string[] args)
        {
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
            // Agent instantiation is initiated from Authority-Manage.The Host does not have control.
            // Returns an agent that has access to all the psuedo local functions
            // Agent has an Agency with experts in their domain.            
            // The Agency functions are only loaded when other Agents send them.

            Console.WriteLine($"{agent.Name} Ready");

            // Here we want to communicate with our local agent.

            Data? message = "Ready For Input";

            while (_host.IsConnected)
            {
                var response = await agent.Runner.DispatchAsync<InteractWithUser>(message);
                message = response.Output;
            }

            Console.WriteLine($"Host Stopped");
        }

        private static async Task GetInputFromUser_callback(Runner runner, Data? output)
        {
            if (((string?)output)?.StartsWith("echo:") ?? false)
            {
                var response = await runner.Echo(((string?)output)?.Substring(5));

                Console.WriteLine(response.Output);
            }

            if (((string?)output)?.StartsWith("log:") ?? false)
            {
                runner.Log(((string?)output)?.Substring(4) ?? string.Empty);
            }

            if (((string?)output)?.StartsWith("web:") ?? false)
            {
                await runner.DispatchAsync("Agience.Agents.Web.Templates.IncomingWebChatMessage", ((string?)output)?.Substring(4));
            }

            if (output == "quit")
            {
                Console.WriteLine("Stopping Host");
                await _host.Stop();
            }
        }
    }
}