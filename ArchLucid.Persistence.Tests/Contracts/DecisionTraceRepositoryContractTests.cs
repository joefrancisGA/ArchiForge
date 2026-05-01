using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for authority <see cref="IDecisionTraceRepository" />.
/// </summary>
public abstract class DecisionTraceRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IDecisionTraceRepository CreateRepository();

    private static DecisionTrace NewTrace(ScopeContext scope, Guid runId, Guid traceId)
    {
        return RuleAuditTrace.From(new RuleAuditTracePayload
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            DecisionTraceId = traceId,
            RunId = runId,
            CreatedUtc = DateTime.UtcNow,
            RuleSetId = "rs-dt",
            RuleSetVersion = "1",
            RuleSetHash = "h",
            AppliedRuleIds = ["r1"],
            AcceptedFindingIds = [],
            RejectedFindingIds = [],
            Notes = ["n1"]
        });
    }

    [Fact]
    public async Task Save_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IDecisionTraceRepository repo = CreateRepository();
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"),
            WorkspaceId = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"),
            ProjectId = Guid.Parse("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d3")
        };

        Guid runId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();

        await PrepareRunForTraceAsync(scope, runId, CancellationToken.None);

        DecisionTrace trace = NewTrace(scope, runId, traceId);
        await repo.SaveAsync(trace, CancellationToken.None);

        DecisionTrace? loaded = await repo.GetByIdAsync(scope, traceId, CancellationToken.None);
        loaded.Should().NotBeNull();
        RuleAuditTracePayload audit = loaded.RequireRuleAudit();
        audit.DecisionTraceId.Should().Be(traceId);
        audit.RunId.Should().Be(runId);
        audit.AppliedRuleIds.Should().Equal("r1");
    }

    [Fact]
    public async Task GetById_wrong_scope_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IDecisionTraceRepository repo = CreateRepository();
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"),
            WorkspaceId = Guid.Parse("e2e2e2e2-e2e2-e2e2-e2e2-e2e2e2e2e2e2"),
            ProjectId = Guid.Parse("e3e3e3e3-e3e3-e3e3-e3e3-e3e3e3e3e3e3")
        };

        Guid runId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();

        await PrepareRunForTraceAsync(scope, runId, CancellationToken.None);

        DecisionTrace trace = NewTrace(scope, runId, traceId);
        await repo.SaveAsync(trace, CancellationToken.None);

        ScopeContext other = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = scope.WorkspaceId, ProjectId = scope.ProjectId
        };

        DecisionTrace? loaded = await repo.GetByIdAsync(other, traceId, CancellationToken.None);
        loaded.Should().BeNull();
    }

    /// <summary>SQL needs <c>dbo.Runs</c> row for FK; in-memory ignores.</summary>
    protected virtual Task PrepareRunForTraceAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        _ = scope;
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }
}
