namespace Agience.Hosts.Service;

internal class Program
{
    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionProcessor;

        var builder = Host.CreateApplicationBuilder(args);

        if (builder.Environment.EnvironmentName == "Development")
        {
            builder.Configuration.AddUserSecrets<Program>();
        }

        var intermediateServiceProvider = builder.Services.BuildServiceProvider();
        var configuration = intermediateServiceProvider.GetRequiredService<IConfiguration>();

        var appConfig = new AppConfig();
        configuration.Bind(appConfig);
        builder.Services.AddSingleton(appConfig);

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }

    static void UnhandledExceptionProcessor(object sender, UnhandledExceptionEventArgs e)
    {
        //Any action here...
        //Implement Logging here...

        //Temp
        Console.WriteLine("\n\n Unhandled Exception: " + e.ExceptionObject.ToString());
    }
}

