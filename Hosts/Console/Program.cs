using Microsoft.Extensions.DependencyInjection;
using Agience.Hosts._Console.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Agience.SDK;

namespace Agience.Hosts._Console
{
    internal class Program
    {
        private static SDK.Host? _host;
        private static string? _contextAgentId;
        private static ILogger<Program>? _logger;

        internal static async Task Main(string[] args)
        {
            // TODO: Handle this all in a separate class

            HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            //builder.Logging.AddDebug();

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionProcessor;

            builder.Configuration.AddUserSecrets<AppConfig>();

            var config = builder.Configuration.Get<AppConfig>();

            if (string.IsNullOrEmpty(config.HostName)) { throw new ArgumentNullException("HostName"); }
            if (string.IsNullOrEmpty(config.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
            if (string.IsNullOrEmpty(config.HostId)) { throw new ArgumentNullException("HostId"); }
            if (string.IsNullOrEmpty(config.HostSecret)) { throw new ArgumentNullException("HostSecret"); }

            // Register local services
            builder.Services.AddSingleton<IConsoleService, ConsoleService>();

            // Add Agience Host
            builder
                .AddAgienceHost(config.HostName, config.AuthorityUri, config.HostId, config.HostSecret, config.CustomNtpHost)

            // TODO: Move the plugins to the Primary Plugins Library and remove the SK dependency. Plugins can load during runtime and per-agent instead.
                .AddAgiencePluginFromType<ConsolePlugin>()
                .AddAgiencePluginFromType<EmailPlugin>()
                .AddAgiencePluginFromType<AuthorEmailPlanner>();

            // TODO: Add plugins from a local assembly directory (startup and runtime)        
            // TODO: Add plugins initiated from Authority (startup and runtime)

            var app = builder.Build();

            _logger = app.Services.GetRequiredService<ILogger<Program>>();

            _host = app.GetAgienceHost();

            if (_host == null) { throw new InvalidOperationException("Host not found"); }

            _host.AgentConnected += _host_AgentConnected;            

            await _host.Run();
        }

        static void UnhandledExceptionProcessor(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError("\n\n Unhandled Exception occurred: " + e.ExceptionObject.ToString());
        }

        private static async Task _host_AgentConnected(Agent agent)
        {
            _logger.LogInformation($"{agent.Name} Ready");

            if (_contextAgentId == null)
            {
                // TODO: Read the input and set the context agent. For now, we will just use the first agent.        

                _contextAgentId = agent.Id;

                _logger.LogInformation($"* Switched context to {_host!.GetAgentById(agent.Id)?.Name ?? "Unknown"} *");

                await RunConsole();
            }
        }

        private static async Task RunConsole()
        {
            // TODO: Add a way to switch context agents

            var contextAgent = _host?.GetAgentById(_contextAgentId);

            if (contextAgent == null) { throw new InvalidOperationException("No context agent set"); }

            string? userInput;

            Console.Write($"User > ");

            while ((userInput = Console.ReadLine()) != null)
            {
                await foreach (var message in _host!.GetAgentById(_contextAgentId)!.ProcessAsync(userInput))
                {
                    Console.WriteLine($"{message.AuthorRole} > {message.Content}");
                }

                Console.Write($"User > ");
            }
        }
    }
}