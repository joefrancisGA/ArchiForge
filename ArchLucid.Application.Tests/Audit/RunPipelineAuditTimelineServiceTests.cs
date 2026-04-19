using ArchLucid.Application.Audit;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Audit;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RunPipelineAuditTimelineServiceTests
{
    [Fact]
    public async Task GetTimelineAsync_returns_null_when_run_missing()
    {
        Guid runId = Guid.NewGuid();
        Mock<IAuthorityQueryService> query = new();
        query
            .Setup(q => q.GetRunSummaryAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunSummaryDto?)null);

        Mock<IAuditRepository> audit = new();
        RunPipelineAuditTimelineService sut = new(query.Object, audit.Object);

        IReadOnlyList<RunPipelineTimelineItemDto>? result = await sut.GetTimelineAsync(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() },
            runId,
            CancellationToken.None);

        result.Should().BeNull();
        audit.Verify(
            a => a.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTimelineAsync_orders_oldest_first()
    {
        Guid runId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        RunSummaryDto summary = new()
        {
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAuthorityQueryService> query = new();
        query
            .Setup(q => q.GetRunSummaryAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        DateTime t1 = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime t2 = new(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc);

        List<AuditEvent> fromDb =
        [
            new AuditEvent
            {
                EventType = "B",
                OccurredUtc = t2,
                ActorUserId = "u",
                ActorUserName = "U",
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                RunId = runId,
            },
            new AuditEvent
            {
                EventType = "A",
                OccurredUtc = t1,
                ActorUserId = "u",
                ActorUserName = "U",
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                RunId = runId,
            },
        ];

        Mock<IAuditRepository> audit = new();
        audit
            .Setup(a => a.GetFilteredAsync(tenantId, workspaceId, projectId, It.IsAny<AuditEventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromDb);

        RunPipelineAuditTimelineService sut = new(query.Object, audit.Object);
        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
        };

        IReadOnlyList<RunPipelineTimelineItemDto>? result = await sut.GetTimelineAsync(scope, runId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Select(r => r.EventType).Should().Equal("A", "B");
    }
}
