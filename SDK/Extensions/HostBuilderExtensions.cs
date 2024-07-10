using Agience.SDK.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agience.SDK.Extensions;
public static class HostBuilderExtensions
{
    public static IHostApplicationBuilder AddAgienceHost(this IHostApplicationBuilder appBuilder,
          string hostName,
          string authorityUri,
          string hostId,
          string hostSecret,
          string? customNtpHost = null)
    {
        appBuilder.Services.AddSingleton(new KernelPluginCollection());
        appBuilder.Services.AddSingleton<PluginRuntimeLoader>();
        appBuilder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
        appBuilder.Services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Authority>>()));
        appBuilder.Services.AddSingleton(x => new AgentFactory(x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x, x.GetRequiredService<KernelPluginCollection>()));
        appBuilder.Services.AddSingleton(x => new Host(hostName, hostId, hostSecret, x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x.GetRequiredService<AgentFactory>(), x.GetRequiredService<PluginRuntimeLoader>(), x.GetRequiredService<ILogger<Host>>()));
        return appBuilder;
    }


    public static IHostBuilder ConfigureAgienceHost(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;
            var hostName = configuration["HostName"]; // TODO: HostName should be provided by the Authority in the welcome message.
            var authorityUri = configuration["AuthorityUri"] ?? throw new ArgumentNullException("AuthorityUri");
            var hostId = configuration["HostId"] ?? throw new ArgumentNullException("HostId");
            var hostSecret = configuration["HostSecret"] ?? throw new ArgumentNullException("HostSecret");
            var customNtpHost = configuration["CustomNtpHost"];

            services.AddSingleton(new KernelPluginCollection());
            services.AddSingleton<PluginRuntimeLoader>();
            services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
            services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<ILogger<Authority>>()));
            services.AddSingleton(x => new AgentFactory(x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x, x.GetRequiredService<KernelPluginCollection>()));
            services.AddSingleton(x => new Host(hostName, hostId, hostSecret, x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x.GetRequiredService<AgentFactory>(), x.GetRequiredService<PluginRuntimeLoader>(), x.GetRequiredService<ILogger<Host>>()));
        });

        return hostBuilder;
    }

    public static IHostApplicationBuilder AddAgienceAuthority(this IHostApplicationBuilder appBuilder,
        string authorityUri,
        string? customNtpHost = null
        )
    {
        appBuilder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
        appBuilder.Services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<IAuthorityDataAdapter>(), x.GetRequiredService<ILogger<Authority>>()));
        return appBuilder;
    }
}
