using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence.Cosmos;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Jobs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AuditEventChangeFeedArchLucidJobTests
{
    [Fact]
    public void Name_is_canonical_audit_change_feed_slug()
    {
        Mock<IAuditEventChangeFeedSingleBatchRunner> processor = new();
        Mock<IOptionsMonitor<CosmosDbOptions>> cosmos = new();
        cosmos.Setup(m => m.CurrentValue).Returns(new CosmosDbOptions { AuditEventsEnabled = false });

        AuditEventChangeFeedArchLucidJob job = new(
            processor.Object,
            cosmos.Object,
            NullLogger<AuditEventChangeFeedArchLucidJob>.Instance);

        job.Name.Should().Be(ArchLucidJobNames.AuditChangeFeed);
    }

    [Fact]
    public async Task RunOnceAsync_skips_processor_when_audit_events_disabled()
    {
        Mock<IAuditEventChangeFeedSingleBatchRunner> processor = new();
        Mock<IOptionsMonitor<CosmosDbOptions>> cosmos = new();
        cosmos.Setup(m => m.CurrentValue).Returns(new CosmosDbOptions { AuditEventsEnabled = false });

        AuditEventChangeFeedArchLucidJob job = new(
            processor.Object,
            cosmos.Object,
            NullLogger<AuditEventChangeFeedArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
        processor.Verify(
            p => p.RunSingleBatchOrIdleAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_returns_job_failure_when_processor_throws()
    {
        Mock<IAuditEventChangeFeedSingleBatchRunner> processor = new();
        processor.Setup(p => p.RunSingleBatchOrIdleAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated change feed failure"));

        Mock<IOptionsMonitor<CosmosDbOptions>> cosmos = new();
        cosmos.Setup(m => m.CurrentValue).Returns(new CosmosDbOptions { AuditEventsEnabled = true });

        AuditEventChangeFeedArchLucidJob job = new(
            processor.Object,
            cosmos.Object,
            NullLogger<AuditEventChangeFeedArchLucidJob>.Instance);

        int code = await job.RunOnceAsync(CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.JobFailure);
    }
}
