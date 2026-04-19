using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence.Archival;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DataArchivalArchLucidJobTests
{
    [Fact]
    public void Name_is_canonical_data_archival_slug()
    {
        Mock<IServiceScopeFactory> scopeFactory = new();
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(new DataArchivalOptions { Enabled = false });

        DataArchivalArchLucidJob job = new(
            scopeFactory.Object,
            options.Object,
            new DataArchivalHostHealthState(),
            NullLogger<DataArchivalArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.DataArchival);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_archival_disabled()
    {
        Mock<IServiceScopeFactory> scopeFactory = new(MockBehavior.Strict);
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(new DataArchivalOptions { Enabled = false });

        DataArchivalArchLucidJob job = new(
            scopeFactory.Object,
            options.Object,
            new DataArchivalHostHealthState(),
            NullLogger<DataArchivalArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    [Fact]
    public async Task RunOnceAsync_returns_job_failure_when_coordinator_throws()
    {
        Mock<IDataArchivalCoordinator> coordinator = new();
        coordinator.Setup(c => c.RunOnceAsync(It.IsAny<DataArchivalOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated archival failure"));

        ServiceCollection services = new();
        services.AddScoped<IDataArchivalCoordinator>(_ => coordinator.Object);

        await using ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(m => m.CurrentValue).Returns(new DataArchivalOptions { Enabled = true });

        DataArchivalArchLucidJob job = new(
            scopeFactory,
            options.Object,
            new DataArchivalHostHealthState(),
            NullLogger<DataArchivalArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.JobFailure);
    }
}
