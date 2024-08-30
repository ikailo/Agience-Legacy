using Agience.SDK.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Agience.Plugins.Primary.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Agience.Plugins.Primary.Interaction;

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

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                logger.LogError($"Unhandled Exception occurred: {e.ExceptionObject}");
            };

            // ** Configure the Agience Host ** //

            var agienceHost = app.Services.GetRequiredService<SDK.Host>();

            agienceHost.Services.AddSingleton<IConsoleService, AgienceConsolePluginService>();

#pragma warning disable SKEXP0050
            // TODO: These plugins should be loaded dynamically during runtime.
            agienceHost.AddPluginFromType<TimePlugin>("msTime");
            agienceHost.AddPluginFromType<ConsolePlugin>("agienceConsole");
            agienceHost.AddPluginFromType<ChatCompletionPlugin>("openAiChatCompletion");
#pragma warning restore SKEXP0050

            // ** Start the Agience Host and Console ** //

            try
            {
                await agienceHost.StartAsync();
                await app.Services.GetRequiredService<AgienceConsoleService>().RunAsync();
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

                    services.AddAgienceHost(config.AuthorityUri, config.HostId, config.HostSecret, config.CustomNtpHost, null, null, config.HostOpenAiApiKey);
                    services.AddSingleton<AgienceConsoleService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}