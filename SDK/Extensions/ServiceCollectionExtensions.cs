using Agience.SDK.Logging;
using Agience.SDK.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    string? brokerUriInternal = null,
    string? hostOpenAiApiKey = null)
        {
            services.AddSingleton(sp => new Authority(authorityUri, sp.GetRequiredService<Broker>(), null, sp.GetRequiredService<ILogger<Authority>>(), authorityUriInternal, brokerUriInternal));

            services.AddSingleton<Authority>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Authority>>();
                var broker = sp.GetRequiredService<Broker>();

                return new Authority(authorityUri, broker, null, logger, authorityUriInternal, brokerUriInternal);
            });

            services.AddSingleton<Broker>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Broker>>();
                return new Broker(logger, customNtpHost);
            });

            services.AddSingleton<AgentFactory>(sp =>
            {
                var serviceProvider = sp.GetRequiredService<IServiceProvider>();
                var authority = sp.GetRequiredService<Authority>();
                var broker = sp.GetRequiredService<Broker>();
                var logger = sp.GetRequiredService<ILogger<AgentFactory>>();
                return new AgentFactory(serviceProvider, authority, broker, logger, hostOpenAiApiKey);
            });

            services.AddSingleton<Host>(sp =>
            {
                var serviceProvider = sp.GetRequiredService<IServiceProvider>();
                var authority = serviceProvider.GetRequiredService<Authority>();
                var broker = serviceProvider.GetRequiredService<Broker>();
                var agentFactory = serviceProvider.GetRequiredService<AgentFactory>();
                var logger = serviceProvider.GetRequiredService<ILogger<Host>>();
                return new Host(hostId, hostSecret, authority, broker, agentFactory, logger);
            });
        }



        public static void AddAgienceAuthority(
            this IServiceCollection services,
            string authorityUri,
            string? customNtpHost = null,
            string? authorityUriInternal = null,
            string? brokerUriInternal = null)
        {
            services.AddSingleton(sp => new Broker(sp.GetRequiredService<ILogger<Broker>>(), customNtpHost));
            services.AddSingleton(sp => new Authority(authorityUri, sp.GetRequiredService<Broker>(), sp.GetRequiredService<IAuthorityDataAdapter>(), sp.GetRequiredService<ILogger<Authority>>(), authorityUriInternal, brokerUriInternal));
        }
    }
}
