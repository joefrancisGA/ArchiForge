using System.Data;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class PolicyPackManagementServiceTests
{
    [Fact]
    public async Task CreatePack_EmitsCreatedChangeLogEntry()
    {
        Mock<IPolicyPackRepository> packRepo = new();
        Mock<IPolicyPackVersionRepository> versionRepo = new();
        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        Mock<IPolicyPackChangeLogRepository> changeLog = new();
        changeLog
            .Setup(
                c => c.AppendAsync(
                    It.IsAny<PolicyPackChangeLogEntry>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        packRepo.Setup(
                p => p.CreateAsync(
                    It.IsAny<PolicyPack>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);
        versionRepo.Setup(
                v => v.CreateAsync(
                    It.IsAny<PolicyPackVersion>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        PolicyPackManagementService sut = new(
            packRepo.Object,
            versionRepo.Object,
            new Mock<IPolicyPackAssignmentRepository>().Object,
            changeLog.Object,
            uowFactory.Object,
            NullLogger<PolicyPackManagementService>.Instance);

        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid workspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid projectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await sut.CreatePackAsync(tenantId, workspaceId, projectId, "MyPack", "d", PolicyPackType.ProjectCustom, "{}", CancellationToken.None);

        changeLog.Verify(
            c => c.AppendAsync(
                It.Is<PolicyPackChangeLogEntry>(
                    e =>
                        e.ChangeType == PolicyPackChangeTypes.Created
                        && e.ChangedBy == "system"
                        && e.SummaryText != null
                        && e.SummaryText.Contains("MyPack", StringComparison.Ordinal)
                        && e.NewValue != null
                        && e.NewValue.Contains("\"name\":\"MyPack\"", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePack_ChangeLogFailure_StillReturnsPack()
    {
        Mock<IPolicyPackRepository> packRepo = new();
        Mock<IPolicyPackVersionRepository> versionRepo = new();
        Mock<IPolicyPackChangeLogRepository> changeLog = new();
        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        packRepo.Setup(
                p => p.CreateAsync(
                    It.IsAny<PolicyPack>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);
        versionRepo.Setup(
                v => v.CreateAsync(
                    It.IsAny<PolicyPackVersion>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        changeLog
            .Setup(
                c => c.AppendAsync(
                    It.IsAny<PolicyPackChangeLogEntry>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .ThrowsAsync(new InvalidOperationException("log failed"));

        PolicyPackManagementService sut = new(
            packRepo.Object,
            versionRepo.Object,
            new Mock<IPolicyPackAssignmentRepository>().Object,
            changeLog.Object,
            uowFactory.Object,
            NullLogger<PolicyPackManagementService>.Instance);

        PolicyPack result = await sut.CreatePackAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "P",
            "",
            PolicyPackType.BuiltIn,
            "{}",
            CancellationToken.None);

        result.Name.Should().Be("P");
    }

    [Fact]
    public async Task PublishVersion_EmitsVersionPublishedChangeLogEntry()
    {
        Guid packId = Guid.NewGuid();
        Mock<IPolicyPackRepository> packRepo = new();
        packRepo.Setup(p => p.GetByIdAsync(packId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PolicyPack
                {
                    PolicyPackId = packId,
                    TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "n",
                    Description = "",
                    PackType = PolicyPackType.BuiltIn,
                    Status = PolicyPackStatus.Draft,
                    CurrentVersion = "0.0.1",
                });
        packRepo.Setup(p => p.UpdateAsync(It.IsAny<PolicyPack>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPolicyPackVersionRepository> versionRepo = new();
        versionRepo.Setup(v => v.GetByPackAndVersionAsync(packId, "2.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyPackVersion?)null);
        versionRepo.Setup(
                v => v.CreateAsync(
                    It.IsAny<PolicyPackVersion>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        Mock<IPolicyPackChangeLogRepository> changeLog = new();
        changeLog
            .Setup(
                c => c.AppendAsync(
                    It.IsAny<PolicyPackChangeLogEntry>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        PolicyPackManagementService sut = new(
            packRepo.Object,
            versionRepo.Object,
            new Mock<IPolicyPackAssignmentRepository>().Object,
            changeLog.Object,
            new Mock<IArchLucidUnitOfWorkFactory>().Object,
            NullLogger<PolicyPackManagementService>.Instance);

        await sut.PublishVersionAsync(packId, "2.0.0", """{"k":1}""", CancellationToken.None);

        changeLog.Verify(
            c => c.AppendAsync(
                It.Is<PolicyPackChangeLogEntry>(
                    e =>
                        e.ChangeType == PolicyPackChangeTypes.VersionPublished
                        && e.ChangedBy == "system"
                        && e.SummaryText != null
                        && e.SummaryText.Contains("2.0.0", StringComparison.Ordinal)
                        && e.NewValue != null
                        && e.NewValue.Contains("\"k\":1", StringComparison.Ordinal)
                        && e.PreviousValue == null),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishVersion_ExistingVersion_IncludesPreviousValue()
    {
        Guid packId = Guid.NewGuid();
        PolicyPackVersion existing = new()
        {
            PolicyPackVersionId = Guid.NewGuid(),
            PolicyPackId = packId,
            Version = "1.0.0",
            ContentJson = """{"old":true}""",
            CreatedUtc = DateTime.UtcNow,
            IsPublished = false,
        };

        Mock<IPolicyPackVersionRepository> versionRepo = new();
        versionRepo.Setup(v => v.GetByPackAndVersionAsync(packId, "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        versionRepo.Setup(v => v.UpdateAsync(It.IsAny<PolicyPackVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPolicyPackRepository> packRepo = new();
        packRepo.Setup(p => p.GetByIdAsync(packId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PolicyPack
                {
                    PolicyPackId = packId,
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    Name = "n",
                    Description = "",
                    PackType = PolicyPackType.BuiltIn,
                    Status = PolicyPackStatus.Draft,
                    CurrentVersion = "1.0.0",
                });
        packRepo.Setup(p => p.UpdateAsync(It.IsAny<PolicyPack>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPolicyPackChangeLogRepository> changeLog = new();
        changeLog
            .Setup(
                c => c.AppendAsync(
                    It.IsAny<PolicyPackChangeLogEntry>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        PolicyPackManagementService sut = new(
            packRepo.Object,
            versionRepo.Object,
            new Mock<IPolicyPackAssignmentRepository>().Object,
            changeLog.Object,
            new Mock<IArchLucidUnitOfWorkFactory>().Object,
            NullLogger<PolicyPackManagementService>.Instance);

        await sut.PublishVersionAsync(packId, "1.0.0", """{"new":true}""", CancellationToken.None);

        changeLog.Verify(
            c => c.AppendAsync(
                It.Is<PolicyPackChangeLogEntry>(
                    e =>
                        e.PreviousValue == """{"old":true}"""
                        && e.NewValue != null
                        && e.NewValue.Contains("\"new\":true", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Once);
    }

    [Fact]
    public async Task Assign_EmitsAssignedChangeLogEntry()
    {
        Mock<IPolicyPackAssignmentRepository> assignmentRepo = new();
        assignmentRepo.Setup(a => a.CreateAsync(It.IsAny<PolicyPackAssignment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPolicyPackChangeLogRepository> changeLog = new();
        changeLog
            .Setup(
                c => c.AppendAsync(
                    It.IsAny<PolicyPackChangeLogEntry>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        PolicyPackManagementService sut = new(
            new Mock<IPolicyPackRepository>().Object,
            new Mock<IPolicyPackVersionRepository>().Object,
            assignmentRepo.Object,
            changeLog.Object,
            new Mock<IArchLucidUnitOfWorkFactory>().Object,
            NullLogger<PolicyPackManagementService>.Instance);

        Guid packId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await sut.AssignAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            packId,
            "1.0.0",
            GovernanceScopeLevel.Project,
            isPinned: true,
            CancellationToken.None);

        changeLog.Verify(
            c => c.AppendAsync(
                It.Is<PolicyPackChangeLogEntry>(
                    e =>
                        e.ChangeType == PolicyPackChangeTypes.Assigned
                        && e.ChangedBy == "system"
                        && e.SummaryText != null
                        && e.SummaryText.Contains("Project", StringComparison.Ordinal)
                        && e.NewValue != null
                        && e.NewValue.Contains("\"isPinned\":true", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Once);
    }

    [Fact]
    public async Task ArchiveAssignment_WhenSuccessful_EmitsArchivedChangeLogEntry()
    {
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid assignmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid packId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        Mock<IPolicyPackAssignmentRepository> assignmentRepo = new();
        assignmentRepo.Setup(a => a.ArchiveAsync(tenantId, assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        assignmentRepo
            .Setup(a => a.GetByTenantAndAssignmentIdAsync(tenantId, assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PolicyPackAssignment
                {
                    AssignmentId = assignmentId,
                    TenantId = tenantId,
                    WorkspaceId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    ProjectId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    PolicyPackId = packId,
                    PolicyPackVersion = "1.0.0",
                    ScopeLevel = GovernanceScopeLevel.Project,
                });

        Mock<IPolicyPackChangeLogRepository> changeLog = new();
        changeLog
            .Setup(
                c => c.AppendAsync(
                    It.IsAny<PolicyPackChangeLogEntry>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .Returns(Task.CompletedTask);

        PolicyPackManagementService sut = new(
            new Mock<IPolicyPackRepository>().Object,
            new Mock<IPolicyPackVersionRepository>().Object,
            assignmentRepo.Object,
            changeLog.Object,
            new Mock<IArchLucidUnitOfWorkFactory>().Object,
            NullLogger<PolicyPackManagementService>.Instance);

        bool ok = await sut.TryArchiveAssignmentAsync(tenantId, assignmentId, CancellationToken.None);

        ok.Should().BeTrue();
        changeLog.Verify(
            c => c.AppendAsync(
                It.Is<PolicyPackChangeLogEntry>(
                    e =>
                        e.ChangeType == PolicyPackChangeTypes.AssignmentArchived
                        && e.SummaryText != null
                        && e.SummaryText.Contains(assignmentId.ToString(), StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Once);
    }

    [Fact]
    public async Task ArchiveAssignment_WhenNotFound_DoesNotEmitChangeLogEntry()
    {
        Guid tenantId = Guid.NewGuid();
        Guid assignmentId = Guid.NewGuid();

        Mock<IPolicyPackAssignmentRepository> assignmentRepo = new();
        assignmentRepo.Setup(a => a.ArchiveAsync(tenantId, assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IPolicyPackChangeLogRepository> changeLog = new();

        PolicyPackManagementService sut = new(
            new Mock<IPolicyPackRepository>().Object,
            new Mock<IPolicyPackVersionRepository>().Object,
            assignmentRepo.Object,
            changeLog.Object,
            new Mock<IArchLucidUnitOfWorkFactory>().Object,
            NullLogger<PolicyPackManagementService>.Instance);

        bool ok = await sut.TryArchiveAssignmentAsync(tenantId, assignmentId, CancellationToken.None);

        ok.Should().BeFalse();
        changeLog.Verify(
            c => c.AppendAsync(
                It.IsAny<PolicyPackChangeLogEntry>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Never);
    }
}
