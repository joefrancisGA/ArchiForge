using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.CustomerSuccess;
using ArchLucid.Persistence.Tenancy;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

using Moq;

namespace ArchLucid.Persistence.Tests.CustomerSuccess;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class TenantHealthScoresBatchRefreshIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task RefreshAllTenantHealthScoresAsync_batch_proc_upserts_row_per_tenant_with_workspace()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository tenants = new(factory);
        Mock<IRlsSessionContextApplicator> rls = new();
        rls.Setup(a => a.ApplyAsync(It.IsAny<SqlConnection>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SqlTenantCustomerSuccessRepository sut = new(factory, rls.Object);
        Guid tenantId = Guid.NewGuid();
        string slug = "hs-" + Guid.NewGuid().ToString("N")[..8];
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        await tenants.InsertTenantAsync(
            tenantId,
            "health-score batch",
            slug,
            TenantTier.Free,
            null,
            CancellationToken.None);

        await tenants.InsertWorkspaceAsync(
            workspaceId,
            tenantId,
            "ws-hs",
            projectId,
            CancellationToken.None);

        await sut.RefreshAllTenantHealthScoresAsync(CancellationToken.None);

        await using SqlConnection read = new(fixture.ConnectionString);
        await read.OpenAsync();
        await PersistenceIntegrationTestRlsSession.ApplyArchLucidRlsBypassAsync(read, CancellationToken.None);

        decimal? composite = await read.ExecuteScalarAsync<decimal?>(
            """
            SELECT CompositeScore FROM dbo.TenantHealthScores WHERE TenantId = @TenantId
            """,
            new { TenantId = tenantId });

        composite.Should().NotBeNull();
        decimal engagement = TenantHealthScoringCalculator.EngagementScore(0, 0, 0);
        decimal breadth = TenantHealthScoringCalculator.BreadthScore(0);
        decimal quality = TenantHealthScoringCalculator.QualityScore(0, 0);
        decimal gov = TenantHealthScoringCalculator.GovernanceScore(0);
        decimal support = TenantHealthScoringCalculator.NeutralSupportScore();
        decimal expected = TenantHealthScoringCalculator.CompositeScore(engagement, breadth, quality, gov, support);
        composite!.Value.Should().Be(expected);
    }
}
