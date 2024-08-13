using Agience.SDK.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agience.SDK.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAgienceHost(
            this IServiceCollection services,
            string authorityUri,
            string hostId,
            string hostSecret,
            string? customNtpHost = null,
            string? authorityUriInternal = null,
            string? brokerUriInternal = null)
        {
            //services.AddSingleton<PluginRuntimeLoader>();
            services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
            services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), null, x.GetRequiredService<ILogger<Authority>>(), authorityUriInternal, brokerUriInternal));
            services.AddSingleton(x => new AgentFactory(x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>()));
            services.AddSingleton(x => new Host(hostId, hostSecret, x.GetRequiredService<Authority>(), x.GetRequiredService<Broker>(), x.GetRequiredService<AgentFactory>(), /*x.GetRequiredService<PluginRuntimeLoader>(),*/ x.GetRequiredService<ILogger<Host>>()));
        }

        public static void AddAgienceAuthority(
            this IServiceCollection services,
            string authorityUri,
            string? customNtpHost = null,
            string? authorityUriInternal = null,
            string? brokerUriInternal = null)
        {
            services.AddSingleton(x => new Broker(x.GetRequiredService<ILogger<Broker>>(), customNtpHost));
            services.AddSingleton(x => new Authority(authorityUri, x.GetRequiredService<Broker>(), x.GetRequiredService<IAuthorityDataAdapter>(), x.GetRequiredService<ILogger<Authority>>(), authorityUriInternal, brokerUriInternal));
        }
    }
}
