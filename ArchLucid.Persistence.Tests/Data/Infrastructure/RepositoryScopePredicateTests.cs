using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class RepositoryScopePredicateTests
{
    [SkippableFact]
    public void AndTripleWhere_empty_tenant_returns_empty_clause()
    {
        ScopeContext scope = new() { TenantId = Guid.Empty, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        string clause = RepositoryScopePredicate.AndTripleWhere(scope);

        clause.Should().BeEmpty();
    }

    [SkippableFact]
    public void AndTripleWhere_non_empty_tenant_appends_scope_predicate_sql()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        string clause = RepositoryScopePredicate.AndTripleWhere(scope);

        clause.Should().Contain("TenantId = @ScopeTenantId");
        clause.Should().Contain("WorkspaceId = @ScopeWorkspaceId");
        clause.Should().Contain("ProjectId = @ScopeProjectId");
    }

    [SkippableFact]
    public void AddScopeTripleIfNeeded_skips_parameters_when_tenant_empty()
    {
        ScopeContext scope = new() { TenantId = Guid.Empty, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
        DynamicParameters dp = new();

        RepositoryScopePredicate.AddScopeTripleIfNeeded(dp, scope);

        Action readTenant = () => _ = dp.Get<Guid>("ScopeTenantId");
        readTenant.Should().Throw<KeyNotFoundException>();
    }

    [SkippableFact]
    public void AddScopeTripleIfNeeded_registers_scope_triple_when_tenant_present()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };
        DynamicParameters dp = new();

        RepositoryScopePredicate.AddScopeTripleIfNeeded(dp, scope);

        dp.Get<Guid>("ScopeTenantId").Should().Be(scope.TenantId);
        dp.Get<Guid>("ScopeWorkspaceId").Should().Be(scope.WorkspaceId);
        dp.Get<Guid>("ScopeProjectId").Should().Be(scope.ProjectId);
    }
}
