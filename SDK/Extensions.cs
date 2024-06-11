using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agience.SDK;
public static class Extensions
{
    public static IHostApplicationBuilder AddAgienceHost(this IHostApplicationBuilder builder,
          string hostName,
          string authorityUri,
          string hostId,
          string hostSecret,
          string? customNtpHost = null)
    {
        builder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
        builder.Services.AddSingleton(x => new Host(hostName, authorityUri, hostId, hostSecret, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Host>>()));
        builder.Services.AddSingleton(new KernelPluginCollection());
        return builder;
    }

    public static IHostApplicationBuilder AddAgiencePluginFromType<T>(this IHostApplicationBuilder builder,
        string? pluginName = null,
        IServiceProvider? serviceProvider = null)
    {
        builder.Services.AddSingleton(x => x.GetService<KernelPluginCollection>().AddFromType<T>(pluginName, serviceProvider));
        return builder;
    }

    public static IHostBuilder ConfigureAgienceHost(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;
            var hostName = configuration["HostName"];
            var authorityUri = configuration["AuthorityUri"];
            var clientId = configuration["HostId"];
            var clientSecret = configuration["HostSecret"];
            var customNtpHost = configuration["CustomNtpHost"];

            services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
            services.AddSingleton(x => new Host(hostName, authorityUri, clientId, clientSecret, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Host>>()));
            services.AddSingleton(new KernelPluginCollection());
        });

        return hostBuilder;
    }


    public static Host? GetAgienceHost(this IHost host) => host.Services.GetService<Host>();

    public static IHostApplicationBuilder AddAgienceAuthority(this IHostApplicationBuilder builder,
        string authorityUri,
        string? customNtpHost = null
        )
    {
        builder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost)); 
        builder.Services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>()));        
        return builder;
    }

    public static Authority? GetAgienceAuthority(this IHost host) => host.Services.GetService<Authority>();
}
