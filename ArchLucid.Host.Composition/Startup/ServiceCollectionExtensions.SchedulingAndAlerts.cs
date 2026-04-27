using ArchLucid.Application.Advisory;
using ArchLucid.Core.Integration;
using ArchLucid.Decisioning.Advisory.Delivery;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Composite;
using ArchLucid.Decisioning.Alerts.Delivery;
using ArchLucid.Decisioning.Alerts.Simulation;
using ArchLucid.Decisioning.Alerts.Tuning;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Contracts.Abstractions.Integrations;
using ArchLucid.Host.Core.Integration;
using ArchLucid.Integrations.AzureDevOps;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Host.Core.Services;
using ArchLucid.Host.Core.Services.Delivery;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Persistence.Orchestration;
using ArchLucid.Persistence.Simulation;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDataArchivalHostedService(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        // Shared by DataArchivalArchLucidJob (registered for every role in RegisterArchLucidJobRunners) and by
        // DataArchivalHostedService / DataArchivalHostHealthCheck on Worker+Combined. Api does not run the in-process
        // archival loop but still composes IArchLucidJob implementations — DI must resolve this singleton.
        services.AddSingleton<DataArchivalHostHealthState>();

        if (hostingRole is not ArchLucidHostingRole.Combined and not ArchLucidHostingRole.Worker)
            return;


        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.DataArchival))

            services.AddHostedService<DataArchivalHostedService>();

    }

    private static void RegisterRetrievalIndexingOutbox(IServiceCollection services, ArchLucidHostingRole hostingRole)
    {
        services.AddSingleton<IRetrievalIndexingOutboxProcessor, RetrievalIndexingOutboxProcessor>();
        services.AddSingleton<IAuthorityPipelineWorkProcessor, AuthorityPipelineWorkProcessor>();

        if (hostingRole is not (ArchLucidHostingRole.Combined or ArchLucidHostingRole.Worker))
            return;

        services.AddHostedService<RetrievalIndexingOutboxHostedService>();
        services.AddHostedService<AuthorityPipelineWorkHostedService>();
    }

    private static void RegisterIntegrationEventOutbox(IServiceCollection services, ArchLucidHostingRole hostingRole)
    {
        services.AddSingleton<IIntegrationEventOutboxProcessor, IntegrationEventOutboxProcessor>();

        if (hostingRole is ArchLucidHostingRole.Combined or ArchLucidHostingRole.Worker)

            services.AddHostedService<IntegrationEventOutboxHostedService>();

    }

    private static void RegisterIntegrationEventConsumer(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        if (hostingRole is not ArchLucidHostingRole.Worker)
            return;


        services.Configure<AzureDevOpsIntegrationOptions>(configuration.GetSection(AzureDevOpsIntegrationOptions.SectionName));
        services.AddHttpClient<IAzureDevOpsPullRequestDecorator, AzureDevOpsPullRequestDecorator>();
        services.AddSingleton<IIntegrationEventHandler, AuthorityRunCompletedAzureDevOpsIntegrationEventHandler>();
        services.AddSingleton<IIntegrationEventHandler, TrialLifecycleEmailIntegrationEventHandler>();
        services.AddSingleton<IIntegrationEventHandler, LoggingIntegrationEventHandler>();

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.ServiceBusIntegrationEvents))

            services.AddHostedService<AzureServiceBusIntegrationEventConsumer>();

    }

    private static void RegisterAdvisoryScheduling(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        services.AddScoped<IScanScheduleCalculator, SimpleScanScheduleCalculator>();
        services.AddScoped<IArchitectureDigestBuilder, ArchitectureDigestBuilder>();
        services.AddScoped<IAdvisoryScanRunner, AdvisoryScanRunner>();
        services.AddScoped<AdvisoryDueScheduleProcessor>();

        if (hostingRole is not (ArchLucidHostingRole.Combined or ArchLucidHostingRole.Worker))
            return;

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.AdvisoryScan))

            services.AddHostedService<AdvisoryScanHostedService>();

    }

    private static void RegisterDigestDelivery(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebhookDeliveryOptions>(configuration.GetSection(WebhookDeliveryOptions.SectionName));
        services.AddSingleton<IEmailSender, FakeEmailSender>();
        services
            .AddHttpClient(
                "ArchLucidWebhooks",
                static client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddSingleton<HttpWebhookPoster>();
        services.AddSingleton<FakeWebhookPoster>();
        services.AddSingleton<IWebhookPoster>(static sp =>
        {
            IOptionsMonitor<WebhookDeliveryOptions> monitor = sp.GetRequiredService<IOptionsMonitor<WebhookDeliveryOptions>>();
            IWebhookPoster inner = monitor.CurrentValue.UseHttpClient
                ? sp.GetRequiredService<HttpWebhookPoster>()
                : sp.GetRequiredService<FakeWebhookPoster>();

            IWebhookPoster withOptionalCloudEvents = new CloudEventsWrappingWebhookPoster(monitor, inner);

            return new WebhookHmacEnvelopePoster(monitor, withOptionalCloudEvents);
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
            string? queueOrTopic = options.QueueOrTopicName?.Trim();
            string? fullyQualifiedNamespace = options.ServiceBusFullyQualifiedNamespace?.Trim();
            string? connectionString = options.ServiceBusConnectionString?.Trim();
            string? managedIdentityClientId = options.ServiceBusManagedIdentityClientId?.Trim();

            if (string.IsNullOrEmpty(queueOrTopic))
                return NullIntegrationEventPublisher.Instance;


            ILogger<AzureServiceBusIntegrationEventPublisher> logger =
                sp.GetRequiredService<ILogger<AzureServiceBusIntegrationEventPublisher>>();

            if (!string.IsNullOrEmpty(fullyQualifiedNamespace))

                return new AzureServiceBusIntegrationEventPublisher(
                    fullyQualifiedNamespace,
                    queueOrTopic,
                    string.IsNullOrEmpty(managedIdentityClientId) ? null : managedIdentityClientId,
                    logger);


            if (!string.IsNullOrEmpty(connectionString))
                return new AzureServiceBusIntegrationEventPublisher(connectionString, queueOrTopic, logger);


            return NullIntegrationEventPublisher.Instance;
        });
    }
}
