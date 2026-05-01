using ArchLucid.Core.Audit;
using ArchLucid.Decisioning.Governance.PolicyPacks;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Policy Packs App Service.
/// </summary>
[Trait("Category", "Unit")]
public sealed class PolicyPacksAppServiceTests
{
    [SkippableFact]
    public async Task CreatePackAsync_WhenManagementSucceeds_AuditsCreated()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        PolicyPack returned = new()
        {
            PolicyPackId = Guid.NewGuid(), Name = "pack-a", PackType = PolicyPackType.BuiltIn
        };

        Mock<IPolicyPackManagementService> management = new();
        management
            .Setup(x => x.CreatePackAsync(
                tenantId,
                workspaceId,
                projectId,
                "n",
                "d",
                PolicyPackType.BuiltIn,
                "{}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returned);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        PolicyPacksAppService sut = new(management.Object, Mock.Of<IPolicyPackVersionRepository>(), audit.Object);

        PolicyPack result = await sut.CreatePackAsync(tenantId, workspaceId, projectId, "n", "d",
            PolicyPackType.BuiltIn, "{}", CancellationToken.None);

        result.Should().BeSameAs(returned);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.PolicyPackCreated),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task TryAssignAsync_WhenVersionMissing_ReturnsNullWithoutAssignOrAudit()
    {
        Guid packId = Guid.NewGuid();
        Mock<IPolicyPackVersionRepository> versions = new();
        versions
            .Setup(x => x.GetByPackAndVersionAsync(packId, "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyPackVersion?)null);

        Mock<IPolicyPackManagementService> management = new(MockBehavior.Strict);
        Mock<IAuditService> audit = new(MockBehavior.Strict);

        PolicyPacksAppService sut = new(management.Object, versions.Object, audit.Object);

        PolicyPackAssignment? result = await sut.TryAssignAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            packId,
            "1.0.0",
            "workspace",
            false,
            CancellationToken.None);

        result.Should().BeNull();
        management.Verify(
            x => x.AssignAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task TryArchiveAssignmentAsync_WhenManagementReturnsTrue_AuditsArchived()
    {
        Guid tenantId = Guid.NewGuid();
        Guid assignmentId = Guid.NewGuid();

        Mock<IPolicyPackManagementService> management = new();
        management
            .Setup(x => x.TryArchiveAssignmentAsync(tenantId, assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        PolicyPacksAppService sut = new(management.Object, Mock.Of<IPolicyPackVersionRepository>(), audit.Object);

        bool ok = await sut.TryArchiveAssignmentAsync(tenantId, assignmentId, CancellationToken.None);

        ok.Should().BeTrue();
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.PolicyPackAssignmentArchived),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
