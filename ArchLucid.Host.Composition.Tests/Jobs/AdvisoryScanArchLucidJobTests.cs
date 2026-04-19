using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AdvisoryScanArchLucidJobTests
{
    [Fact]
    public async Task Name_is_canonical_advisory_scan_slug()
    {
        Mock<IAdvisoryScanScheduleRepository> repo = new();
        ServiceCollection services = new();
        services.AddSingleton(repo.Object);
        services.AddSingleton<IAdvisoryScanRunner>(Mock.Of<IAdvisoryScanRunner>());
        services.AddSingleton<ILogger<AdvisoryDueScheduleProcessor>>(
            _ => NullLogger<AdvisoryDueScheduleProcessor>.Instance);
        services.AddScoped<AdvisoryDueScheduleProcessor>();

        await using ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        AdvisoryScanArchLucidJob job = new(scopeFactory, NullLogger<AdvisoryScanArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.AdvisoryScan);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_no_due_schedules()
    {
        Mock<IAdvisoryScanScheduleRepository> repo = new();
        repo.Setup(r => r.ListDueAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AdvisoryScanSchedule>());

        ServiceCollection services = new();
        services.AddSingleton(repo.Object);
        services.AddSingleton<IAdvisoryScanRunner>(Mock.Of<IAdvisoryScanRunner>());
        services.AddSingleton<ILogger<AdvisoryDueScheduleProcessor>>(
            _ => NullLogger<AdvisoryDueScheduleProcessor>.Instance);
        services.AddScoped<AdvisoryDueScheduleProcessor>();

        await using ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        AdvisoryScanArchLucidJob job = new(scopeFactory, NullLogger<AdvisoryScanArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    [Fact]
    public async Task RunOnceAsync_returns_job_failure_when_list_due_throws()
    {
        Mock<IAdvisoryScanScheduleRepository> repo = new();
        repo.Setup(r => r.ListDueAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated repository failure"));

        ServiceCollection services = new();
        services.AddSingleton(repo.Object);
        services.AddSingleton<IAdvisoryScanRunner>(Mock.Of<IAdvisoryScanRunner>());
        services.AddSingleton<ILogger<AdvisoryDueScheduleProcessor>>(
            _ => NullLogger<AdvisoryDueScheduleProcessor>.Instance);
        services.AddScoped<AdvisoryDueScheduleProcessor>();

        await using ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        AdvisoryScanArchLucidJob job = new(scopeFactory, NullLogger<AdvisoryScanArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.JobFailure);
    }
}
