using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     SQL integration tests for trial run counter increments co-located with <see cref="SqlRunRepository.SaveAsync" />.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class DapperRunRepositoryTrialIncrementTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task SaveAsync_parallel_creates_respect_trial_run_cap()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);
        SqlRunRepository repo = SqlRunRepositoryTestFactory.Create(sqlFactory, listFactory);

        Guid tenantId = Guid.NewGuid();
        string slug = "trial_run_" + Guid.NewGuid().ToString("N");

        await using (SqlConnection setup = new(fixture.ConnectionString))
        {
            await setup.OpenAsync(CancellationToken.None);

            await setup.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, TrialStartUtc, TrialExpiresUtc, TrialRunsLimit, TrialRunsUsed, TrialSeatsLimit, TrialSeatsUsed, TrialStatus, TrialSampleRunId)
                    VALUES (@Id, @Name, @Slug, N'Standard', SYSUTCDATETIME(), DATEADD(day, 30, SYSUTCDATETIME()), @RunLimit, 0, 10, 1, @Active, NEWID());
                    """,
                    new
                    {
                        Id = tenantId,
                        Name = "Trial Tenant",
                        Slug = slug,
                        RunLimit = 5,
                        TrialLifecycleStatus.Active
                    },
                    cancellationToken: CancellationToken.None));
        }

        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        int successes = 0;
        int trialFailures = 0;
        Task[] tasks = new Task[6];

        for (int i = 0; i < tasks.Length; i++)
        {
            int idx = i;
            tasks[idx] = Task.Run(async () =>
            {
                RunRecord run = new()
                {
                    RunId = Guid.NewGuid(),
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    ProjectId = "p_" + idx,
                    Description = "trial cap",
                    CreatedUtc = DateTime.UtcNow
                };

                try
                {
                    await repo.SaveAsync(run, CancellationToken.None);
                    Interlocked.Increment(ref successes);
                }
                catch (TrialLimitExceededException)
                {
                    Interlocked.Increment(ref trialFailures);
                }
            });
        }

        await Task.WhenAll(tasks);

        successes.Should().Be(5);
        trialFailures.Should().Be(1);

        await using SqlConnection verify = new(fixture.ConnectionString);
        await verify.OpenAsync(CancellationToken.None);

        int used = await verify.QuerySingleAsync<int>(
            new CommandDefinition(
                "SELECT TrialRunsUsed FROM dbo.Tenants WHERE Id = @Id;",
                new { Id = tenantId },
                cancellationToken: CancellationToken.None));

        used.Should().Be(5);
    }

    [Fact]
    public async Task SaveAsync_does_not_increment_when_trial_not_active()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);
        SqlRunRepository repo = SqlRunRepositoryTestFactory.Create(sqlFactory, listFactory);

        Guid tenantId = Guid.NewGuid();
        string slug = "no_trial_" + Guid.NewGuid().ToString("N");

        await using (SqlConnection setup = new(fixture.ConnectionString))
        {
            await setup.OpenAsync(CancellationToken.None);

            await setup.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, TrialRunsUsed, TrialSeatsUsed)
                    VALUES (@Id, @Name, @Slug, N'Standard', 0, 1);
                    """,
                    new { Id = tenantId, Name = "No Trial", Slug = slug },
                    cancellationToken: CancellationToken.None));
        }

        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        RunRecord run = new()
        {
            RunId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "p",
            Description = "no trial",
            CreatedUtc = DateTime.UtcNow
        };

        await repo.SaveAsync(run, CancellationToken.None);

        await using SqlConnection verify = new(fixture.ConnectionString);
        await verify.OpenAsync(CancellationToken.None);

        int used = await verify.QuerySingleAsync<int>(
            new CommandDefinition(
                "SELECT TrialRunsUsed FROM dbo.Tenants WHERE Id = @Id;",
                new { Id = tenantId },
                cancellationToken: CancellationToken.None));

        used.Should().Be(0);
    }
}
