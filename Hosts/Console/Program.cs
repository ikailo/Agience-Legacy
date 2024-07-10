using Agience.SDK.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agience.Hosts._Console
{
    internal class Program
    {
        private static ILogger<Program>? _logger;

        internal static async Task Main(string[] args)
        {
            var appBuilder = Host.CreateApplicationBuilder(args);

            appBuilder.Logging.ClearProviders();
            appBuilder.Logging.AddConsole();
            
            // appBuilder.Logging.AddDebug();

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionProcessor;            

            appBuilder.Configuration.AddUserSecrets<AppConfig>();

            var config = appBuilder.Configuration.Get<AppConfig>() ?? new AppConfig();

            // TODO: These checks might not be necessary since we'll check in the builder anyway
            if (string.IsNullOrWhiteSpace(config.HostName)) { throw new ArgumentNullException("HostName"); }
            if (string.IsNullOrWhiteSpace(config.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
            if (string.IsNullOrWhiteSpace(config.HostId)) { throw new ArgumentNullException("HostId"); }
            if (string.IsNullOrWhiteSpace(config.HostSecret)) { throw new ArgumentNullException("HostSecret"); }
            if (string.IsNullOrWhiteSpace(config.OpenAiApiKey)) { throw new ArgumentNullException("OpenAiApiKey"); }

            // Add Agience Host
            appBuilder.AddAgienceHost(config.HostName, config.AuthorityUri, config.HostId, config.HostSecret, config.CustomNtpHost);
                        
            // TODO: Load services from Authority, specific to Agent.
            appBuilder.Services.AddSingleton<IChatCompletionService>(new OpenAIChatCompletionService("gpt-3.5-turbo", config.OpenAiApiKey));

            appBuilder.Services.AddTransient<AgienceConsoleHost>();

            var app = appBuilder.Build();

            _logger = app.Services.GetRequiredService<ILogger<Program>>();

#pragma warning disable SKEXP0050
            app.Services.GetRequiredService<KernelPluginCollection>().AddFromType<TimePlugin>("time");
#pragma warning restore SKEXP0050

            // TODO: Add plugins from a local assembly directory (startup and runtime)
            // TODO: Add plugins initiated from Authority (startup and runtime)
            // TODO: Register local services and plugins.
            // appBuilder.Services.AddSingleton<IConsoleService>(new ConsoleService());

            await app.Services.GetRequiredService<AgienceConsoleHost>().Run();
        }

        static void UnhandledExceptionProcessor(object sender, UnhandledExceptionEventArgs e)
        {
            _logger?.LogError($"\n\n Unhandled Exception occurred: {e.ExceptionObject}");
        }
    }
}