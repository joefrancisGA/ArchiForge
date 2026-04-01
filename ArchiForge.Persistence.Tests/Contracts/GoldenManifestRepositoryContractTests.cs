using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for authority <see cref="IGoldenManifestRepository"/> (SQL + in-memory).
/// </summary>
public abstract class GoldenManifestRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IGoldenManifestRepository CreateRepository();

    private static GoldenManifest NewMinimalManifest(
        ScopeContext scope,
        Guid runId,
        Guid contextId,
        Guid graphId,
        Guid findingsId,
        Guid traceId,
        Guid manifestId)
    {
        return new GoldenManifest
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            FindingsSnapshotId = findingsId,
            DecisionTraceId = traceId,
            CreatedUtc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
            ManifestHash = "contract-hash",
            RuleSetId = "rs-contract",
            RuleSetVersion = "1",
            RuleSetHash = "rsh-contract",
            Metadata = new ManifestMetadata { Name = "Contract Manifest" },
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = ["a1"],
            Warnings = ["w1"],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };
    }

    [SkippableFact]
    public async Task Save_then_GetById_round_trips_core_fields()
    {
        SkipIfSqlServerUnavailable();
        IGoldenManifestRepository repo = CreateRepository();
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
            WorkspaceId = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"),
            ProjectId = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"),
        };

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await PrepareAuthorityChainForManifestAsync(
            scope,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            CancellationToken.None);

        GoldenManifest manifest = NewMinimalManifest(scope, runId, contextId, graphId, findingsId, traceId, manifestId);
        await repo.SaveAsync(manifest, CancellationToken.None);

        GoldenManifest? loaded = await repo.GetByIdAsync(scope, manifestId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.ManifestId.Should().Be(manifestId);
        loaded.RunId.Should().Be(runId);
        loaded.Metadata.Name.Should().Be("Contract Manifest");
        loaded.Assumptions.Should().Equal("a1");
    }

    [SkippableFact]
    public async Task GetById_wrong_scope_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IGoldenManifestRepository repo = CreateRepository();
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"),
            WorkspaceId = Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2"),
            ProjectId = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3"),
        };

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await PrepareAuthorityChainForManifestAsync(
            scope,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            CancellationToken.None);

        GoldenManifest manifest = NewMinimalManifest(scope, runId, contextId, graphId, findingsId, traceId, manifestId);
        await repo.SaveAsync(manifest, CancellationToken.None);

        ScopeContext otherTenant = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
        };

        GoldenManifest? loaded = await repo.GetByIdAsync(otherTenant, manifestId, CancellationToken.None);
        loaded.Should().BeNull();
    }

    /// <summary>
    /// SQL subclasses seed FK chain; in-memory is a no-op.
    /// </summary>
    protected virtual Task PrepareAuthorityChainForManifestAsync(
        ScopeContext scope,
        Guid runId,
        Guid contextId,
        Guid graphId,
        Guid findingsId,
        Guid traceId,
        CancellationToken ct)
    {
        _ = scope;
        _ = runId;
        _ = contextId;
        _ = graphId;
        _ = findingsId;
        _ = traceId;
        _ = ct;

        return Task.CompletedTask;
    }
}
