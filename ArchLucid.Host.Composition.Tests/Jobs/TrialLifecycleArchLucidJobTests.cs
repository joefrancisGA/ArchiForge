using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TrialLifecycleArchLucidJobTests
{
    [Fact]
    public async Task Name_is_canonical_trial_lifecycle_slug()
    {
        await using ServiceProvider provider = BuildProviderWithEmptyAutomationList();
        TrialLifecycleArchLucidJob job = new(provider, NullLogger<TrialLifecycleArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.TrialLifecycle);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_no_automation_tenants()
    {
        await using ServiceProvider provider = BuildProviderWithEmptyAutomationList();
        TrialLifecycleArchLucidJob job = new(provider, NullLogger<TrialLifecycleArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    [Fact]
    public async Task RunOnceAsync_returns_job_failure_when_tenant_repository_resolution_throws()
    {
        ServiceCollection services = [];
        services.AddScoped<ITenantRepository>(_ => throw new InvalidOperationException("simulated DI failure"));

        await using ServiceProvider provider = services.BuildServiceProvider();
        TrialLifecycleArchLucidJob job = new(provider, NullLogger<TrialLifecycleArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.JobFailure);
    }

    private static ServiceProvider BuildProviderWithEmptyAutomationList()
    {
        Mock<ITenantRepository> tenantRepo = new();
        tenantRepo.Setup(r => r.ListTrialLifecycleAutomationTenantIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IOptionsMonitor<TrialLifecycleSchedulerOptions>> lifecycleOptions = new();
        lifecycleOptions.Setup(m => m.CurrentValue).Returns(new TrialLifecycleSchedulerOptions());

        ServiceCollection services = [];
        services.AddScoped<ITenantRepository>(_ => tenantRepo.Object);
        services.AddScoped<ITenantHardPurgeService>(_ => Mock.Of<ITenantHardPurgeService>());
        services.AddScoped<IAuditService>(_ => Mock.Of<IAuditService>());
        services.AddSingleton(lifecycleOptions.Object);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILogger<TrialLifecycleTransitionEngine>>(_ =>
            NullLogger<TrialLifecycleTransitionEngine>.Instance);
        services.AddScoped<TrialLifecycleTransitionEngine>();

        return services.BuildServiceProvider();
    }
}
