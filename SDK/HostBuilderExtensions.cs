using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agience.SDK;
public static class HostBuilderExtensions
{
    public static IHostApplicationBuilder AddAgienceHost(this IHostApplicationBuilder builder,
        string name,
        string authorityUri,
        string clientId,
        string clientSecret,
        string? brokerUriOverride = null,
        string? customNtpHost = null)
    {
        builder.Services.AddSingleton<KernelPluginCollection>();     
        builder.Services.AddSingleton<PluginRuntimeLoader>();      
        builder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost)); 
        builder.Services.AddSingleton(x => new Host(name, authorityUri, clientId, clientSecret, x.GetRequiredService<Broker>(), x.GetRequiredService<PluginRuntimeLoader>(), brokerUriOverride));
        return builder;
    }

    public static IHost AddAgiencePlugin<T>(this IHost host,
        string? pluginName = null,
        IServiceProvider? serviceProvider = null)
    {
        host.Services.GetRequiredService<KernelPluginCollection>().AddFromType<T>(pluginName, serviceProvider);
        return host;
    }

    public static Host GetAgieceHost(this IHost host) => host.Services.GetRequiredService<Host>();

}
