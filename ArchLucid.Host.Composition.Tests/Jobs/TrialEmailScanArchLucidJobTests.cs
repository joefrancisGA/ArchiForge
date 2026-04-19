using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TrialEmailScanArchLucidJobTests
{
    [Fact]
    public async Task Name_is_canonical_trial_email_scan_slug()
    {
        await using ServiceProvider provider = BuildProviderWithEmptyTenantList();
        TrialEmailScanArchLucidJob job = new(provider, NullLogger<TrialEmailScanArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.TrialEmailScan);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_no_tenants()
    {
        await using ServiceProvider provider = BuildProviderWithEmptyTenantList();
        TrialEmailScanArchLucidJob job = new(provider, NullLogger<TrialEmailScanArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    [Fact]
    public async Task RunOnceAsync_returns_job_failure_when_scanner_throws()
    {
        Mock<ITenantRepository> tenantRepo = new();
        tenantRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated list failure"));

        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventsOptions = new();
        integrationEventsOptions.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions());

        ServiceCollection services = new();
        services.AddScoped<ITenantRepository>(_ => tenantRepo.Object);
        services.AddScoped<IIntegrationEventOutboxRepository>(_ => Mock.Of<IIntegrationEventOutboxRepository>());
        services.AddScoped<IIntegrationEventPublisher>(_ => Mock.Of<IIntegrationEventPublisher>());
        services.AddSingleton(integrationEventsOptions.Object);
        services.AddScoped<TrialScheduledLifecycleEmailScanner>();
        services.AddSingleton<ILogger<TrialScheduledLifecycleEmailScanner>>(
            _ => NullLogger<TrialScheduledLifecycleEmailScanner>.Instance);

        await using ServiceProvider provider = services.BuildServiceProvider();
        TrialEmailScanArchLucidJob job = new(provider, NullLogger<TrialEmailScanArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.JobFailure);
    }

    private static ServiceProvider BuildProviderWithEmptyTenantList()
    {
        Mock<ITenantRepository> tenantRepo = new();
        tenantRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TenantRecord>());

        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventsOptions = new();
        integrationEventsOptions.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions());

        ServiceCollection services = new();
        services.AddScoped<ITenantRepository>(_ => tenantRepo.Object);
        services.AddScoped<IIntegrationEventOutboxRepository>(_ => Mock.Of<IIntegrationEventOutboxRepository>());
        services.AddScoped<IIntegrationEventPublisher>(_ => Mock.Of<IIntegrationEventPublisher>());
        services.AddSingleton(integrationEventsOptions.Object);
        services.AddScoped<TrialScheduledLifecycleEmailScanner>();
        services.AddSingleton<ILogger<TrialScheduledLifecycleEmailScanner>>(
            _ => NullLogger<TrialScheduledLifecycleEmailScanner>.Instance);

        return services.BuildServiceProvider();
    }
}
