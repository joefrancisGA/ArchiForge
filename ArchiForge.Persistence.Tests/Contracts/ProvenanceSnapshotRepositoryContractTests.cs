using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Provenance;
using ArchiForge.Provenance;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IProvenanceSnapshotRepository"/>.
/// </summary>
public abstract class ProvenanceSnapshotRepositoryContractTests
{
    protected abstract IProvenanceSnapshotRepository CreateRepository();

    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static readonly Guid TenantId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
    private static readonly Guid WorkspaceId = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
    private static readonly Guid ScopeProjectId = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3");

    private static ScopeContext NewScope() =>
        new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ScopeProjectId
        };

    private static DecisionProvenanceSnapshot NewSnapshot(Guid runId, string graphJson, DateTime createdUtc)
    {
        return new DecisionProvenanceSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ScopeProjectId,
            RunId = runId,
            GraphJson = graphJson,
            CreatedUtc = createdUtc
        };
    }

    [SkippableFact]
    public async Task Save_then_GetByRunId_returns_snapshot()
    {
        SkipIfSqlServerUnavailable();
        IProvenanceSnapshotRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        Guid runId = Guid.NewGuid();

        DecisionProvenanceSnapshot snap = NewSnapshot(runId, """{"n":1}""", DateTime.UtcNow);

        await repo.SaveAsync(snap, CancellationToken.None);

        DecisionProvenanceSnapshot? loaded = await repo.GetByRunIdAsync(scope, runId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RunId.Should().Be(runId);
        loaded.GraphJson.Should().Be(snap.GraphJson);
        loaded.TenantId.Should().Be(TenantId);
    }

    [SkippableFact]
    public async Task GetByRunId_wrong_scope_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IProvenanceSnapshotRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        Guid runId = Guid.NewGuid();

        await repo.SaveAsync(NewSnapshot(runId, "{}", DateTime.UtcNow), CancellationToken.None);

        ScopeContext other = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = WorkspaceId,
            ProjectId = ScopeProjectId
        };

        DecisionProvenanceSnapshot? loaded = await repo.GetByRunIdAsync(other, runId, CancellationToken.None);

        loaded.Should().BeNull();
    }

    [SkippableFact]
    public async Task Save_twice_same_run_latest_wins_on_read()
    {
        SkipIfSqlServerUnavailable();
        IProvenanceSnapshotRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        Guid runId = Guid.NewGuid();

        await repo.SaveAsync(NewSnapshot(runId, "first", DateTime.UtcNow.AddMinutes(-2)), CancellationToken.None);
        await repo.SaveAsync(NewSnapshot(runId, "second", DateTime.UtcNow), CancellationToken.None);

        DecisionProvenanceSnapshot? loaded = await repo.GetByRunIdAsync(scope, runId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.GraphJson.Should().Be("second");
    }
}
