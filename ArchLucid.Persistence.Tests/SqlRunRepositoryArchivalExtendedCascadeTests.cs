using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Verifies <see cref="SqlRunRepository" /> archival sets <c>ArchivedUtc</c> on
///     <c>dbo.ArtifactBundles</c>, <c>dbo.AgentExecutionTraces</c>, and <c>dbo.ComparisonRecords</c> when migration 073
///     columns exist.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Persistence")]
public sealed class SqlRunRepositoryArchivalExtendedCascadeTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task ArchiveRunsByIdsAsync_cascades_ArchivedUtc_to_artifact_bundle_agent_trace_and_comparison_record()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection probe = new(fixture.ConnectionString);
        await probe.OpenAsync(CancellationToken.None);

        Skip.IfNot(
            await ExtendedArchiveColumnsExistAsync(probe, CancellationToken.None),
            "Migration 073 ArchivedUtc columns on ArtifactBundles, AgentExecutionTraces, ComparisonRecords are required.");

        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);
        SqlRunRepository repo = SqlRunRepositoryTestFactory.Create(sqlFactory, listFactory);

        ScopeContext scope = NewScope();
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid decisionTraceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        Guid bundleId = Guid.NewGuid();
        string traceId = "trace-ext-" + Guid.NewGuid().ToString("N");
        string taskId = "task-ext-" + Guid.NewGuid().ToString("N");
        string comparisonRecordId = "cmp-ext-" + Guid.NewGuid().ToString("N");
        string slug = "ext_cascade_" + Guid.NewGuid().ToString("N");
        string runIdText = runId.ToString("D");

        RunRecord run = NewRun(scope, runId, slug, DateTime.UtcNow);
        await repo.SaveAsync(run, CancellationToken.None);

        await using SqlConnection seed = new(fixture.ConnectionString);
        await seed.OpenAsync(CancellationToken.None);

        await AuthorityRunChainTestSeed.SeedSnapshotChainForExistingRunAsync(
            seed,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            decisionTraceId,
            slug,
            CancellationToken.None);

        const string insertManifest = """
                                      IF NOT EXISTS (SELECT 1 FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId)
                                      INSERT INTO dbo.GoldenManifests
                                      (ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                       CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                       MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                       ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson, WarningsJson, ProvenanceJson,
                                       TenantId, WorkspaceId, ProjectId)
                                      VALUES
                                      (@ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                                       SYSUTCDATETIME(), N'h', N'rs', N'1', N'rh', N'{}', N'{}', N'{}', N'{}', N'{}', N'{}',
                                       N'{}', N'{}', N'[]', N'[]', N'[]', N'{}',
                                       @TenantId, @WorkspaceId, @ScopeProjectId);
                                      """;

        await seed.ExecuteAsync(
            new CommandDefinition(
                insertManifest,
                new
                {
                    ManifestId = manifestId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    FindingsSnapshotId = findingsId,
                    DecisionTraceId = decisionTraceId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId
                },
                cancellationToken: CancellationToken.None));

        const string insertBundle = """
                                    IF NOT EXISTS (SELECT 1 FROM dbo.ArtifactBundles WHERE BundleId = @BundleId)
                                    INSERT INTO dbo.ArtifactBundles
                                    (BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson, TenantId, WorkspaceId, ProjectId)
                                    VALUES
                                    (@BundleId, @RunId, @ManifestId, SYSUTCDATETIME(), NULL, NULL, @TenantId, @WorkspaceId, @ScopeProjectId);
                                    """;

        await seed.ExecuteAsync(
            new CommandDefinition(
                insertBundle,
                new
                {
                    BundleId = bundleId,
                    RunId = runId,
                    ManifestId = manifestId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId
                },
                cancellationToken: CancellationToken.None));

        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = runIdText,
            AgentType = AgentType.Topology,
            Objective = "test",
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            EvidenceBundleRef = null
        };

        await ArchitectureCommitTestSeed.InsertAgentTaskAsync(seed, task, CancellationToken.None);

        const string insertTrace = """
                                   IF NOT EXISTS (SELECT 1 FROM dbo.AgentExecutionTraces WHERE TraceId = @TraceId)
                                   INSERT INTO dbo.AgentExecutionTraces
                                   (TraceId, RunId, TaskId, AgentType, ParseSucceeded, ErrorMessage, TraceJson, CreatedUtc)
                                   VALUES
                                   (@TraceId, @RunId, @TaskId, N'Topology', 1, NULL, N'{}', SYSUTCDATETIME());
                                   """;

        await seed.ExecuteAsync(
            new CommandDefinition(
                insertTrace,
                new { TraceId = traceId, RunId = runIdText, TaskId = taskId },
                cancellationToken: CancellationToken.None));

        const string insertComparison = """
                                        IF NOT EXISTS (SELECT 1 FROM dbo.ComparisonRecords WHERE ComparisonRecordId = @ComparisonRecordId)
                                        INSERT INTO dbo.ComparisonRecords
                                        (ComparisonRecordId, ComparisonType, LeftRunId, RightRunId, Format, PayloadJson, CreatedUtc)
                                        VALUES
                                        (@ComparisonRecordId, N'GoldenManifest', @LeftRunId, NULL, N'markdown', N'{}', SYSUTCDATETIME());
                                        """;

        await seed.ExecuteAsync(
            new CommandDefinition(
                insertComparison,
                new { ComparisonRecordId = comparisonRecordId, LeftRunId = runIdText },
                cancellationToken: CancellationToken.None));

        RunArchiveByIdsResult result = await repo.ArchiveRunsByIdsAsync([runId], CancellationToken.None);

        result.SucceededRunIds.Should().ContainSingle().Which.Should().Be(runId);

        DateTime? bundleArchived =
            await ReadArchivedUtcAsync(seed, "dbo.ArtifactBundles", "BundleId", bundleId, CancellationToken.None);
        DateTime? comparisonArchived = await ReadArchivedUtcNvarcharKeyAsync(
            seed,
            "dbo.ComparisonRecords",
            "ComparisonRecordId",
            comparisonRecordId,
            CancellationToken.None);

        string traceArchivedSql = """
                                  SELECT ArchivedUtc FROM dbo.AgentExecutionTraces WHERE TraceId = @TraceId;
                                  """;

        DateTime? traceArchived = await seed.QuerySingleOrDefaultAsync<DateTime?>(
            new CommandDefinition(traceArchivedSql, new { TraceId = traceId },
                cancellationToken: CancellationToken.None));

        bundleArchived.Should().NotBeNull("ArtifactBundles.ArchivedUtc should be set with the run archival batch.");
        traceArchived.Should()
            .NotBeNull("AgentExecutionTraces.ArchivedUtc should be set when RunId parses as the archived run.");
        comparisonArchived.Should()
            .NotBeNull("ComparisonRecords.ArchivedUtc should be set when LeftRunId matches the archived run.");
    }

    private static async Task<bool> ExtendedArchiveColumnsExistAsync(SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
                           SELECT CASE
                               WHEN COL_LENGTH(N'dbo.ArtifactBundles', N'ArchivedUtc') IS NOT NULL
                                AND COL_LENGTH(N'dbo.AgentExecutionTraces', N'ArchivedUtc') IS NOT NULL
                                AND COL_LENGTH(N'dbo.ComparisonRecords', N'ArchivedUtc') IS NOT NULL
                               THEN 1 ELSE 0 END;
                           """;

        int flag = await connection.QuerySingleAsync<int>(new CommandDefinition(sql, cancellationToken: ct));

        return flag == 1;
    }

    private static ScopeContext NewScope()
    {
        return new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
    }

    private static RunRecord NewRun(ScopeContext scope, Guid runId, string projectSlug, DateTime createdUtc)
    {
        return new RunRecord
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = projectSlug,
            Description = "extended archival cascade test",
            CreatedUtc = createdUtc
        };
    }

    private static async Task<DateTime?> ReadArchivedUtcAsync(
        SqlConnection connection,
        string tableSql,
        string idColumn,
        Guid id,
        CancellationToken ct)
    {
        string sql = $"""
                      SELECT ArchivedUtc FROM {tableSql}
                      WHERE {idColumn} = @Id;
                      """;

        return await connection.QuerySingleOrDefaultAsync<DateTime?>(new CommandDefinition(sql, new { Id = id },
            cancellationToken: ct));
    }

    private static async Task<DateTime?> ReadArchivedUtcNvarcharKeyAsync(
        SqlConnection connection,
        string tableSql,
        string idColumn,
        string id,
        CancellationToken ct)
    {
        string sql = $"""
                      SELECT ArchivedUtc FROM {tableSql}
                      WHERE {idColumn} = @Id;
                      """;

        return await connection.QuerySingleOrDefaultAsync<DateTime?>(new CommandDefinition(sql, new { Id = id },
            cancellationToken: ct));
    }
}
