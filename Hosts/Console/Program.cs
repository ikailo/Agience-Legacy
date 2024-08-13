using Agience.SDK.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Agience.Plugins.Primary._Console;
using Microsoft.SemanticKernel.Plugins.Core;

namespace Agience.Hosts._Console
{
    internal class Program
    {
        private static ILogger<Program>? _logger;

        internal static async Task Main(string[] args)
        {
            var app = CreateHostBuilder(args).Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            // ** Configure the Agience Host ** //

            var host = app.Services.GetRequiredService<SDK.Host>();

#pragma warning disable SKEXP0050
            // These plugins could/should be loaded dynamically.
            host.AddPluginFromType<TimePlugin>("ms.time");
            host.AddPluginFromType<ConsolePlugin>("agience.console");
#pragma warning restore SKEXP0050

            host.AgentConnected += async (agent) =>
                {
                    logger.LogInformation($"{agent.Name} Connected");
                    //await app.Services.GetRequiredService<AgienceConsoleService>().WriteLineAsync($"{agent.Name} Ready");                                        
                    //await agent.PromptAsync("")
                };

            // ** Done configuring the Agience Host ** //

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    logger.LogError($"Unhandled Exception occurred: {e.ExceptionObject}");
                };

            try
            {
                await host.Run();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while running the application.");
            }
        }        

        private static IHostBuilder CreateHostBuilder(string[] args) =>

            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddUserSecrets<Program>();
                })
                .ConfigureServices((context, services) =>
                {
                    var config = context.Configuration.Get<AppConfig>() ?? new AppConfig();

                    if (string.IsNullOrWhiteSpace(config.AuthorityUri)) { throw new ArgumentNullException(nameof(config.AuthorityUri)); }
                    if (string.IsNullOrWhiteSpace(config.HostId)) { throw new ArgumentNullException(nameof(config.HostId)); }
                    if (string.IsNullOrWhiteSpace(config.HostSecret)) { throw new ArgumentNullException(nameof(config.HostSecret)); }

                    services.AddAgienceHost(config.AuthorityUri, config.HostId, config.HostSecret, config.CustomNtpHost);
                    
                    // Add services to support the plugins.
                    services.AddSingleton<AgienceConsoleService>();

                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}