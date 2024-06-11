using Agience.SDK;

namespace Agience.Hosts.Service;

internal class Program
{
    private static SDK.Host? _host;
    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionProcessor;

        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Configuration.AddUserSecrets<AppConfig>();

        var config = builder.Configuration.Get<AppConfig>();

        if (string.IsNullOrEmpty(config?.HostName)) { throw new ArgumentNullException("HostName"); }
        if (string.IsNullOrEmpty(config?.AuthorityUri)) { throw new ArgumentNullException("AuthorityUri"); }
        if (string.IsNullOrEmpty(config?.HostId)) { throw new ArgumentNullException("HostId"); }
        if (string.IsNullOrEmpty(config?.HostSecret)) { throw new ArgumentNullException("HostSecret"); }

        var intermediateServiceProvider = builder.Services.BuildServiceProvider();
        
        builder.Services.AddHostedService<Worker>();

        builder.AddAgienceHost(config.HostName, config.AuthorityUri, config.HostId, config.HostSecret);
        
        // TODO: Do these at runtime?
        // .AddAgiencePlugin<ConsolePlugin>()
          // .AddAgiencePlugin<EmailPlugin>()
          // .AddAgiencePlugin<AuthorEmailPlanner>();

        var app = builder.Build();
        app.Run();
    }

    static void UnhandledExceptionProcessor(object sender, UnhandledExceptionEventArgs e)
    {
        //Any action here...
        //Implement Logging here...

        //Temp
        Console.WriteLine("\n\n Unhandled Exception occurred: " + e.ExceptionObject.ToString());
    }
}

