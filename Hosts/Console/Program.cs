using Agience.SDK.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Agience.Plugins.Primary._Console;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel;

namespace Agience.Hosts._Console
{
    internal class Program
    {
        private static ILogger<Program>? _logger;
        private static string _contextAgentId = string.Empty;

        internal static async Task Main(string[] args)
        {
            var app = CreateHostBuilder(args).Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            // ** Configure the Agience Host ** //

            var host = app.Services.GetRequiredService<SDK.Host>();

            host.Services.AddSingleton<IConsoleService, AgienceConsoleService>();

#pragma warning disable SKEXP0050
            // TODO: These plugins should be loaded dynamically during runtime.
            host.AddPluginFromType<TimePlugin>("msTime");
            host.AddPluginFromType<ConsolePlugin>("agienceConsole");
#pragma warning restore SKEXP0050

            host.AgentConnected += async (agent) =>
                {
                    if (string.IsNullOrEmpty(_contextAgentId))
                    {
                        _contextAgentId = agent.Id;

                        var args = new KernelArguments();
                        args.Add("message", "input>");
                        await agent.Kernel.InvokeAsync("agienceConsole", "InteractWithPerson", args);
                    }
                };

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
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}