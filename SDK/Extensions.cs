using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agience.SDK;
public static class Extensions
{
    public static IHostApplicationBuilder AddAgienceHost(this IHostApplicationBuilder appBuilder,
          string hostName,
          string authorityUri,
          string hostId,
          string hostSecret,
          string? customNtpHost = null)
    {
        appBuilder.Services.AddSingleton(new KernelPluginCollection());
        appBuilder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
        appBuilder.Services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Authority>>()));
        appBuilder.Services.AddSingleton(x => new AgentFactory(x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x, x.GetRequiredService<KernelPluginCollection>()));
        appBuilder.Services.AddSingleton(x => new Host(hostName, hostId, hostSecret, x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x.GetRequiredService<AgentFactory>(), x.GetRequiredService<ILogger<Host>>()));
        return appBuilder;
    }

    public static IHostApplicationBuilder AddAgiencePluginFromType<T>(this IHostApplicationBuilder appBuilder,
        string? pluginName = null,
        IServiceProvider? serviceProvider = null)
    {
        appBuilder.Services.AddSingleton(x => x.GetService<KernelPluginCollection>().AddFromType<T>(pluginName, serviceProvider));

        return appBuilder;
    }

    public static IHostBuilder ConfigureAgienceHost(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;
            var hostName = configuration["HostName"] ?? throw new ArgumentNullException("HostName");
            var authorityUri = configuration["AuthorityUri"];
            var hostId = configuration["HostId"];
            var hostSecret = configuration["HostSecret"];
            var customNtpHost = configuration["CustomNtpHost"];

            services.AddSingleton(new KernelPluginCollection());            
            services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
            services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Authority>>()));
            services.AddSingleton(x => new AgentFactory(x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x, x.GetRequiredService<KernelPluginCollection>()));
            services.AddSingleton(x => new Host(hostName, hostId, hostSecret, x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x.GetRequiredService<AgentFactory>() ,x.GetRequiredService<ILogger<Host>>()));
            
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
        builder.Services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Authority>>()));        
        return builder;
    }

    public static Authority? GetAgienceAuthority(this IHost host) => host.Services.GetService<Authority>();
}
