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
        builder.Services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost)); 
        builder.Services.AddSingleton(x => new Host(name, authorityUri, clientId, clientSecret, x.GetRequiredService<Broker>(), brokerUriOverride));
        builder.Services.AddSingleton(new KernelPluginCollection());
        return builder;
    }

    public static IHostApplicationBuilder AddAgiencePlugin<T>(this IHostApplicationBuilder builder,
        string? pluginName = null,
        IServiceProvider? serviceProvider = null)
    {
        builder.Services.AddSingleton(x => x.GetService<KernelPluginCollection>().AddFromType<T>(pluginName, serviceProvider));
        return builder;
    }

    public static Host GetAgieceHost(this IHost host) => host.Services.GetService<Host>();

}
