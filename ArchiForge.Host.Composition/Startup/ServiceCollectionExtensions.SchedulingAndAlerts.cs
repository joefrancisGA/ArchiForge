using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Alerts.Simulation;
using ArchiForge.Decisioning.Alerts.Tuning;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Hosted;
using ArchiForge.Host.Core.Hosting;
using ArchiForge.Core.Integration;

using ArchiForge.Host.Core.Integration;
using ArchiForge.Host.Core.Services;
using ArchiForge.Host.Core.Services.Delivery;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Alerts;
using ArchiForge.Persistence.Alerts.Simulation;
using ArchiForge.Persistence.Orchestration;
using ArchiForge.Persistence.Retrieval;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Composition;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDataArchivalHostedService(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        if (hostingRole is not ArchiForgeHostingRole.Combined and not ArchiForgeHostingRole.Worker)
        {
            return;
        }

        services.AddSingleton<DataArchivalHostHealthState>();
        services.AddHostedService<DataArchivalHostedService>();
    }

    private static void RegisterRetrievalIndexingOutbox(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        services.AddSingleton<IRetrievalIndexingOutboxProcessor, RetrievalIndexingOutboxProcessor>();
        services.AddSingleton<IAuthorityPipelineWorkProcessor, AuthorityPipelineWorkProcessor>();

        if (hostingRole is ArchiForgeHostingRole.Combined or ArchiForgeHostingRole.Worker)
        {
            services.AddHostedService<RetrievalIndexingOutboxHostedService>();
            services.AddHostedService<AuthorityPipelineWorkHostedService>();
        }
    }

    private static void RegisterAdvisoryScheduling(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        services.AddScoped<IScanScheduleCalculator, SimpleScanScheduleCalculator>();
        services.AddScoped<IArchitectureDigestBuilder, ArchitectureDigestBuilder>();
        services.AddScoped<IAdvisoryScanRunner, AdvisoryScanRunner>();
        services.AddScoped<AdvisoryDueScheduleProcessor>();

        if (hostingRole is ArchiForgeHostingRole.Combined or ArchiForgeHostingRole.Worker)
        {
            services.AddHostedService<AdvisoryScanHostedService>();
        }
    }

    private static void RegisterDigestDelivery(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebhookDeliveryOptions>(configuration.GetSection(WebhookDeliveryOptions.SectionName));
        services.AddSingleton<IEmailSender, FakeEmailSender>();
        services
            .AddHttpClient(
                "ArchiForgeWebhooks",
                static client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddSingleton<HttpWebhookPoster>();
        services.AddSingleton<FakeWebhookPoster>();
        services.AddSingleton<IWebhookPoster>(static sp =>
        {
            IOptionsMonitor<WebhookDeliveryOptions> monitor = sp.GetRequiredService<IOptionsMonitor<WebhookDeliveryOptions>>();
            IWebhookPoster impl = monitor.CurrentValue.UseHttpClient
                ? sp.GetRequiredService<HttpWebhookPoster>()
                : sp.GetRequiredService<FakeWebhookPoster>();

            return new WebhookHmacEnvelopePoster(monitor, impl);
        });
        services.AddScoped<IDigestDeliveryChannel, DigestEmailDeliveryChannel>();
        services.AddScoped<IDigestDeliveryChannel, DigestTeamsWebhookDeliveryChannel>();
        services.AddScoped<IDigestDeliveryChannel, DigestSlackWebhookDeliveryChannel>();
        services.AddScoped<IDigestDeliveryDispatcher, DigestDeliveryDispatcher>();
    }

    private static void RegisterAlerts(IServiceCollection services)
    {
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();
        services.AddScoped<IAlertDeliveryChannel, AlertEmailDeliveryChannel>();
        services.AddScoped<IAlertDeliveryChannel, AlertTeamsWebhookDeliveryChannel>();
        services.AddScoped<IAlertDeliveryChannel, AlertSlackWebhookDeliveryChannel>();
        services.AddScoped<IAlertDeliveryChannel, AlertOnCallWebhookDeliveryChannel>();
        services.AddScoped<IAlertDeliveryDispatcher, AlertDeliveryDispatcher>();
        services.AddScoped<IAlertService, AlertService>();

        services.AddScoped<IAlertMetricSnapshotBuilder, AlertMetricSnapshotBuilder>();
        services.AddScoped<ICompositeAlertRuleEvaluator, CompositeAlertRuleEvaluator>();
        services.AddScoped<IAlertSuppressionPolicy, AlertSuppressionPolicy>();
        services.AddScoped<ICompositeAlertService, CompositeAlertService>();

        services.AddScoped<IAlertSimulationContextProvider, AlertSimulationContextProvider>();
        services.AddScoped<IRuleSimulationService, RuleSimulationService>();

        services.AddScoped<IAlertNoiseScorer, AlertNoiseScorer>();
        services.AddScoped<IThresholdRecommendationService, ThresholdRecommendationService>();

        services.AddScoped<IPolicyPackResolver, PolicyPackResolver>();
        services.AddScoped<IPolicyPackManagementService, PolicyPackManagementService>();
        services.AddScoped<IEffectiveGovernanceResolver, EffectiveGovernanceResolver>();
        services.AddScoped<EffectiveGovernanceLoader>();
        services.AddScoped<IEffectiveGovernanceLoader>(static sp =>
            new RequestScopedCachingEffectiveGovernanceLoader(sp.GetRequiredService<EffectiveGovernanceLoader>()));
        services.AddScoped<IPolicyPacksAppService, PolicyPacksAppService>();
    }

    private static void RegisterIntegrationEventPublishing(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationEventsOptions>(configuration.GetSection(IntegrationEventsOptions.SectionName));

        services.AddSingleton<IIntegrationEventPublisher>(static sp =>
        {
            IntegrationEventsOptions options = sp.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value;
            string? connectionString = options.ServiceBusConnectionString?.Trim();
            string? queueOrTopic = options.QueueOrTopicName?.Trim();

            if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(queueOrTopic))
            {
                return new AzureServiceBusIntegrationEventPublisher(
                    connectionString,
                    queueOrTopic,
                    sp.GetRequiredService<ILogger<AzureServiceBusIntegrationEventPublisher>>());
            }

            return NullIntegrationEventPublisher.Instance;
        });
    }
}
