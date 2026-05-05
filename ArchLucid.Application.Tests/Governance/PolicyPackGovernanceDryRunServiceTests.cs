using System.Collections.Immutable;

using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Repositories;
using ArchLucid.Persistence.Governance;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class PolicyPackGovernanceDryRunServiceTests
{
    private static readonly ScopeContext TestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Fact]
    public async Task EvaluateAsync_by_run_blocks_when_critical_and_enforcement_requested()
    {
        Guid runGuid = Guid.NewGuid();
        Guid snapshotId = Guid.NewGuid();
        InMemoryRunRepository runs = new();
        await runs.SaveAsync(
            new RunRecord
            {
                RunId = runGuid,
                TenantId = TestScope.TenantId,
                WorkspaceId = TestScope.WorkspaceId,
                ScopeProjectId = TestScope.ProjectId,
                ProjectId = "default",
                ArchitectureRequestId = "req-dry-1",
                LegacyRunStatus = "ReadyForCommit",
                FindingsSnapshotId = snapshotId,
                CreatedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        InMemoryFindingsSnapshotRepository findingsRepo = new();
        await findingsRepo.SaveAsync(
            new FindingsSnapshot
            {
                FindingsSnapshotId = snapshotId,
                RunId = runGuid,
                ContextSnapshotId = Guid.NewGuid(),
                GraphSnapshotId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                Findings =
                [
                    new Finding
                    {
                        FindingId = "f-crit",
                        FindingType = "Compliance",
                        Category = "c",
                        EngineType = "e",
                        Severity = FindingSeverity.Critical,
                        Title = "t",
                        Rationale = "r",
                    },
                ],
            },
            CancellationToken.None);

        PolicyPackGovernanceDryRunServiceTestsFixture fixture = CreateSut(
            runs,
            findingsRepo,
            new InMemoryGoldenManifestRepository(),
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }));

        PolicyPackGovernanceDryRunResult? result = await fixture.Sut.EvaluateAsync(
            """{"metadata":{"governance.blockCommitOnCritical":"true"},"complianceRuleIds":[],"complianceRuleKeys":[],"alertRuleIds":[],"compositeAlertRuleIds":[],"advisoryDefaults":{}}""",
            runGuid.ToString("N"),
            null,
            null,
            null,
            null,
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.GateResult.Blocked.Should().BeTrue();
        result.FailedChecks.Should().ContainSingle();

        fixture.Audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.GovernanceDryRunRequested &&
                    e.DataJson!.Contains("\"workflow\":\"proposedPolicyPackContent\"", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_by_manifest_resolves_run_under_scope()
    {
        Guid runGuid = Guid.NewGuid();
        Guid manifestGuid = Guid.NewGuid();
        Guid snapshotId = Guid.NewGuid();

        InMemoryRunRepository runs = new();
        await runs.SaveAsync(
            new RunRecord
            {
                RunId = runGuid,
                TenantId = TestScope.TenantId,
                WorkspaceId = TestScope.WorkspaceId,
                ScopeProjectId = TestScope.ProjectId,
                ProjectId = "default",
                ArchitectureRequestId = "req-dry-2",
                LegacyRunStatus = "ReadyForCommit",
                FindingsSnapshotId = snapshotId,
                CreatedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        InMemoryFindingsSnapshotRepository findingsRepo = new();
        await findingsRepo.SaveAsync(
            new FindingsSnapshot
            {
                FindingsSnapshotId = snapshotId,
                RunId = runGuid,
                ContextSnapshotId = Guid.NewGuid(),
                GraphSnapshotId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                Findings = [],
            },
            CancellationToken.None);

        InMemoryGoldenManifestRepository manifests = new();
        ManifestDocument manifest = new()
        {
            ManifestId = manifestGuid,
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ProjectId = TestScope.ProjectId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = snapshotId,
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
        };

        await manifests.SaveAsync(manifest, CancellationToken.None);

        PolicyPackGovernanceDryRunServiceTestsFixture fixture = CreateSut(
            runs,
            findingsRepo,
            manifests,
            Options.Create(new PreCommitGovernanceGateOptions()));

        PolicyPackGovernanceDryRunResult? result = await fixture.Sut.EvaluateAsync(
            "{}",
            null,
            manifestGuid,
            true,
            (int)FindingSeverity.Critical,
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ResolvedRunId.Should().Be(runGuid.ToString("N"));
        result.TargetManifestId.Should().Be(manifestGuid);
        result.GateResult.Blocked.Should().BeFalse();

        fixture.Audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.GovernanceDryRunRequested),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_returns_null_when_manifest_out_of_scope()
    {
        InMemoryRunRepository runs = new();
        InMemoryFindingsSnapshotRepository findingsRepo = new();
        InMemoryGoldenManifestRepository manifests = new();
        Guid otherProject = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        Guid manifestGuid = Guid.NewGuid();
        Guid runGuid = Guid.NewGuid();

        ManifestDocument manifest = new()
        {
            ManifestId = manifestGuid,
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ProjectId = otherProject,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
        };

        await manifests.SaveAsync(manifest, CancellationToken.None);

        PolicyPackGovernanceDryRunServiceTestsFixture fixture = CreateSut(
            runs,
            findingsRepo,
            manifests,
            Options.Create(new PreCommitGovernanceGateOptions()));

        PolicyPackGovernanceDryRunResult? result = await fixture.Sut.EvaluateAsync(
            "{}",
            null,
            manifestGuid,
            true,
            null,
            null,
            CancellationToken.None);

        result.Should().BeNull();

        fixture.Audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed record PolicyPackGovernanceDryRunServiceTestsFixture(
        PolicyPackGovernanceDryRunService Sut,
        Mock<IAuditService> Audit);

    private static PolicyPackGovernanceDryRunServiceTestsFixture CreateSut(
        IRunRepository runs,
        IFindingsSnapshotRepository findings,
        IGoldenManifestRepository goldenManifests,
        IOptions<PreCommitGovernanceGateOptions> options)
    {
        Mock<IPromptRedactor> redactor = new();
        redactor
            .Setup(r => r.Redact(It.IsAny<string?>()))
            .Returns((string? s) => new PromptRedactionOutcome(s ?? string.Empty, ImmutableDictionary<string, int>.Empty));

        Mock<IAuditService> audit = new();
        audit
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        PolicyPackGovernanceDryRunService sut = new(
            scope.Object,
            runs,
            findings,
            goldenManifests,
            options,
            redactor.Object,
            audit.Object,
            NullLogger<PolicyPackGovernanceDryRunService>.Instance);

        return new PolicyPackGovernanceDryRunServiceTestsFixture(sut, audit);
    }
}
