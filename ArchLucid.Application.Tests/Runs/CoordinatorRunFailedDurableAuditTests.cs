using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Runs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CoordinatorRunFailedDurableAuditTests
{
    [Fact]
    public async Task TryLogAsync_writes_CoordinatorRunFailed_when_audit_succeeds()
    {
        Mock<IAuditService> audit = new();
        audit
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IScopeContextProvider> scopeProvider = new();
        Guid tenant = Guid.NewGuid();
        Guid workspace = Guid.NewGuid();
        Guid project = Guid.NewGuid();

        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = tenant, WorkspaceId = workspace, ProjectId = project });

        await CoordinatorRunFailedDurableAudit.TryLogAsync(
            audit.Object,
            scopeProvider.Object,
            NullLogger.Instance,
            "actor-1",
            Guid.NewGuid().ToString("D"),
            "unit-test reason",
            CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.CoordinatorRunFailed
                    && e.ActorUserId == "actor-1"
                    && e.TenantId == tenant
                    && e.WorkspaceId == workspace
                    && e.ProjectId == project
                    && e.DataJson.Contains("unit-test reason", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.Run.Failed
                    && e.ActorUserId == "actor-1"
                    && e.TenantId == tenant),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryLogAsync_swallows_audit_exceptions()
    {
        Mock<IAuditService> audit = new();
        audit
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
            });

        Func<Task> act = async () => await CoordinatorRunFailedDurableAudit.TryLogAsync(
            audit.Object,
            scopeProvider.Object,
            NullLogger.Instance,
            "actor",
            "not-a-guid-run-id",
            "reason",
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
