using ArchLucid.Host.Core.DataConsistency;
using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class OrphanProbeArchLucidJobTests
{
    [Fact]
    public void Name_is_canonical_orphan_probe_slug()
    {
        Mock<IDataConsistencyOrphanProbeExecutor> executor = new();
        OrphanProbeArchLucidJob job = new(executor.Object, NullLogger<OrphanProbeArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.OrphanProbe);
    }

    [Fact]
    public async Task RunOnceAsync_returns_success_when_executor_completes()
    {
        Mock<IDataConsistencyOrphanProbeExecutor> executor = new();
        executor.Setup(e => e.RunOnceAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        OrphanProbeArchLucidJob job = new(executor.Object, NullLogger<OrphanProbeArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
    }

    [Fact]
    public async Task RunOnceAsync_returns_job_failure_when_executor_throws()
    {
        Mock<IDataConsistencyOrphanProbeExecutor> executor = new();
        executor.Setup(e => e.RunOnceAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated probe failure"));

        OrphanProbeArchLucidJob job = new(executor.Object, NullLogger<OrphanProbeArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.JobFailure);
    }
}
