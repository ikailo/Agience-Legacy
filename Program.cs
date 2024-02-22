namespace Agience.Agents.Primary
{
    internal class Program
    {
        private static void Main(string[] args)
        {
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
    }
}