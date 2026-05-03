using ArchLucid.Application.Governance.FindingReview;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;

using Moq;

using PersistenceFindingReviewEventRecord = ArchLucid.Persistence.Models.FindingReviewEventRecord;

namespace ArchLucid.Application.Tests.Governance.FindingReview;

/// <seealso cref="FindingReviewTrailAppendService" />
public sealed class FindingReviewTrailAppendServiceTests
{
    [SkippableFact]
    public async Task AppendAsync_calls_trail_then_audit_for_approve_action()
    {
        Mock<IFindingReviewTrailRepository> trails = new();
        Mock<IAuditService> audit = new();

        FindingReviewTrailAppendService sut = new(trails.Object, audit.Object);

        Guid tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        PersistenceFindingReviewEventRecord record = new()
        {
            EventId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-ffffffffffff"),
            TenantId = tenantId,
            WorkspaceId = tenantId,
            ProjectId = tenantId,
            FindingId = "finding-001",
            ReviewerUserId = "eve",
            Action = "Approved",
            OccurredAtUtc = DateTime.UtcNow,
        };

        await sut.AppendAsync(record, CancellationToken.None);

        trails.Verify(r => r.AppendAsync(record, It.IsAny<CancellationToken>()), Times.Once);
        trails.Verify(r => r.ListByFindingAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        audit.Verify(
            s => s.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.FindingReviewApproved &&
                    e.ActorUserId == "eve" &&
                    e.RunId == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task AppendAsync_throws_when_action_unknown()
    {
        FindingReviewTrailAppendService sut = new(Mock.Of<IFindingReviewTrailRepository>(), Mock.Of<IAuditService>());
        PersistenceFindingReviewEventRecord record = new()
        {
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FindingId = "x",
            ReviewerUserId = "eve",
            Action = "Mystery",
            OccurredAtUtc = DateTime.UtcNow,
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(Act);
        return;

        Task Act() => sut.AppendAsync(record, CancellationToken.None);
    }
}
