using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.GoldenManifests;
using ArchiForge.Persistence.RelationalRead;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed <see cref="IGoldenManifestRepository"/> with dual-write to legacy JSON columns and
/// phase-1 relational tables for assumptions, warnings, decisions (+ evidence/node links + RawDecisionJson),
/// and provenance reference lists. Reads prefer relational slices per collection when rows exist.
/// </summary>
public sealed class SqlGoldenManifestRepository(ISqlConnectionFactory connectionFactory) : IGoldenManifestRepository
{
    public async Task SaveAsync(
        GoldenManifest manifest,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (connection is not null)
        {
            await SaveCoreAsync(manifest, connection, transaction, ct);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction tx = owned.BeginTransaction();

        try
        {
            await SaveCoreAsync(manifest, owned, tx, ct);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task SaveCoreAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.GoldenManifests
            (
                TenantId, WorkspaceId, ProjectId,
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                WarningsJson, ProvenanceJson
            )
            VALUES
            (
                @TenantId, @WorkspaceId, @ProjectId,
                @ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                @CreatedUtc, @ManifestHash, @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @MetadataJson, @RequirementsJson, @TopologyJson, @SecurityJson, @ComplianceJson, @CostJson,
                @ConstraintsJson, @UnresolvedIssuesJson, @DecisionsJson, @AssumptionsJson,
                @WarningsJson, @ProvenanceJson
            );
            """;

        object args = new
        {
            manifest.TenantId,
            manifest.WorkspaceId,
            manifest.ProjectId,
            manifest.ManifestId,
            manifest.RunId,
            manifest.ContextSnapshotId,
            manifest.GraphSnapshotId,
            manifest.FindingsSnapshotId,
            manifest.DecisionTraceId,
            manifest.CreatedUtc,
            manifest.ManifestHash,
            manifest.RuleSetId,
            manifest.RuleSetVersion,
            manifest.RuleSetHash,
            MetadataJson = JsonEntitySerializer.Serialize(manifest.Metadata),
            RequirementsJson = JsonEntitySerializer.Serialize(manifest.Requirements),
            TopologyJson = JsonEntitySerializer.Serialize(manifest.Topology),
            SecurityJson = JsonEntitySerializer.Serialize(manifest.Security),
            ComplianceJson = JsonEntitySerializer.Serialize(manifest.Compliance),
            CostJson = JsonEntitySerializer.Serialize(manifest.Cost),
            ConstraintsJson = JsonEntitySerializer.Serialize(manifest.Constraints),
            UnresolvedIssuesJson = JsonEntitySerializer.Serialize(manifest.UnresolvedIssues),
            DecisionsJson = JsonEntitySerializer.Serialize(manifest.Decisions),
            AssumptionsJson = JsonEntitySerializer.Serialize(manifest.Assumptions),
            WarningsJson = JsonEntitySerializer.Serialize(manifest.Warnings),
            ProvenanceJson = JsonEntitySerializer.Serialize(manifest.Provenance),
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));

        await InsertRelationalPhase1Async(manifest, connection, transaction, ct);
    }

    private static async Task InsertRelationalPhase1Async(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        await InsertGoldenManifestAssumptionsRelationalAsync(manifest, connection, transaction, ct);
        await InsertGoldenManifestWarningsRelationalAsync(manifest, connection, transaction, ct);
        await InsertGoldenManifestProvSourceFindingsRelationalAsync(manifest, connection, transaction, ct);
        await InsertGoldenManifestProvSourceGraphNodesRelationalAsync(manifest, connection, transaction, ct);
        await InsertGoldenManifestProvAppliedRulesRelationalAsync(manifest, connection, transaction, ct);
        await InsertGoldenManifestDecisionsRelationalAsync(manifest, connection, transaction, ct);
    }

    private static async Task InsertGoldenManifestAssumptionsRelationalAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid manifestId = manifest.ManifestId;

        const string insertAssumptionSql = """
            INSERT INTO dbo.GoldenManifestAssumptions (ManifestId, SortOrder, AssumptionText)
            VALUES (@ManifestId, @SortOrder, @AssumptionText);
            """;

        for (int i = 0; i < manifest.Assumptions.Count; i++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertAssumptionSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = i,
                        AssumptionText = manifest.Assumptions[i],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertGoldenManifestWarningsRelationalAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid manifestId = manifest.ManifestId;

        const string insertWarningSql = """
            INSERT INTO dbo.GoldenManifestWarnings (ManifestId, SortOrder, WarningText)
            VALUES (@ManifestId, @SortOrder, @WarningText);
            """;

        for (int w = 0; w < manifest.Warnings.Count; w++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertWarningSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = w,
                        WarningText = manifest.Warnings[w],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertGoldenManifestProvSourceFindingsRelationalAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid manifestId = manifest.ManifestId;
        List<string> provFindingIds = manifest.Provenance.SourceFindingIds;

        const string insertProvFindingSql = """
            INSERT INTO dbo.GoldenManifestProvenanceSourceFindings (ManifestId, SortOrder, FindingId)
            VALUES (@ManifestId, @SortOrder, @FindingId);
            """;

        for (int p = 0; p < provFindingIds.Count; p++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertProvFindingSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = p,
                        FindingId = provFindingIds[p],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertGoldenManifestProvSourceGraphNodesRelationalAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid manifestId = manifest.ManifestId;
        List<string> provNodeIds = manifest.Provenance.SourceGraphNodeIds;

        const string insertProvNodeSql = """
            INSERT INTO dbo.GoldenManifestProvenanceSourceGraphNodes (ManifestId, SortOrder, NodeId)
            VALUES (@ManifestId, @SortOrder, @NodeId);
            """;

        for (int p = 0; p < provNodeIds.Count; p++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertProvNodeSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = p,
                        NodeId = provNodeIds[p],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertGoldenManifestProvAppliedRulesRelationalAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid manifestId = manifest.ManifestId;
        List<string> provRuleIds = manifest.Provenance.AppliedRuleIds;

        const string insertProvRuleSql = """
            INSERT INTO dbo.GoldenManifestProvenanceAppliedRules (ManifestId, SortOrder, RuleId)
            VALUES (@ManifestId, @SortOrder, @RuleId);
            """;

        for (int p = 0; p < provRuleIds.Count; p++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertProvRuleSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = p,
                        RuleId = provRuleIds[p],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertGoldenManifestDecisionsRelationalAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid manifestId = manifest.ManifestId;

        const string insertDecisionSql = """
            INSERT INTO dbo.GoldenManifestDecisions
            (
                ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson
            )
            VALUES
            (
                @ManifestId, @SortOrder, @DecisionId, @Category, @Title, @SelectedOption, @Rationale, @RawDecisionJson
            );
            """;

        const string insertEvidenceSql = """
            INSERT INTO dbo.GoldenManifestDecisionEvidenceLinks (ManifestId, DecisionId, SortOrder, FindingId)
            VALUES (@ManifestId, @DecisionId, @SortOrder, @FindingId);
            """;

        const string insertNodeLinkSql = """
            INSERT INTO dbo.GoldenManifestDecisionNodeLinks (ManifestId, DecisionId, SortOrder, NodeId)
            VALUES (@ManifestId, @DecisionId, @SortOrder, @NodeId);
            """;

        for (int d = 0; d < manifest.Decisions.Count; d++)
        {
            ResolvedArchitectureDecision decision = manifest.Decisions[d];

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertDecisionSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = d,
                        decision.DecisionId,
                        decision.Category,
                        decision.Title,
                        decision.SelectedOption,
                        decision.Rationale,
                        decision.RawDecisionJson,
                    },
                    transaction,
                    cancellationToken: ct));

            for (int e = 0; e < decision.SupportingFindingIds.Count; e++)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertEvidenceSql,
                        new
                        {
                            ManifestId = manifestId,
                            decision.DecisionId,
                            SortOrder = e,
                            FindingId = decision.SupportingFindingIds[e],
                        },
                        transaction,
                        cancellationToken: ct));
            }

            for (int n = 0; n < decision.RelatedNodeIds.Count; n++)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertNodeLinkSql,
                        new
                        {
                            ManifestId = manifestId,
                            decision.DecisionId,
                            SortOrder = n,
                            NodeId = decision.RelatedNodeIds[n],
                        },
                        transaction,
                        cancellationToken: ct));
            }
        }
    }

    public async Task<GoldenManifest?> GetByIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
            SELECT
                TenantId, WorkspaceId, ProjectId,
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                WarningsJson, ProvenanceJson
            FROM dbo.GoldenManifests
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND ManifestId = @ManifestId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(
                sql,
                new
                {
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId,
                    ManifestId = manifestId,
                },
                cancellationToken: ct));

        if (row is null)
            return null;

        return await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, ct);
    }

    /// <summary>
    /// Inserts phase-1 relational slices that are still empty while JSON columns contain data (idempotent per slice).
    /// </summary>
    internal static async Task BackfillPhase1RelationalSlicesAsync(
        GoldenManifest manifest,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(connection);

        Guid manifestId = manifest.ManifestId;

        int assumptionsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestAssumptions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int warningsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestWarnings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provFindingCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceFindings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provNodeCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceGraphNodes WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provRuleCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceAppliedRules WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int decisionsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestDecisions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        if (assumptionsCount == 0 && manifest.Assumptions.Count > 0)
            await InsertGoldenManifestAssumptionsRelationalAsync(manifest, connection, transaction, ct);

        if (warningsCount == 0 && manifest.Warnings.Count > 0)
            await InsertGoldenManifestWarningsRelationalAsync(manifest, connection, transaction, ct);

        if (provFindingCount == 0 && manifest.Provenance.SourceFindingIds.Count > 0)
            await InsertGoldenManifestProvSourceFindingsRelationalAsync(manifest, connection, transaction, ct);

        if (provNodeCount == 0 && manifest.Provenance.SourceGraphNodeIds.Count > 0)
            await InsertGoldenManifestProvSourceGraphNodesRelationalAsync(manifest, connection, transaction, ct);

        if (provRuleCount == 0 && manifest.Provenance.AppliedRuleIds.Count > 0)
            await InsertGoldenManifestProvAppliedRulesRelationalAsync(manifest, connection, transaction, ct);

        if (decisionsCount == 0 && manifest.Decisions.Count > 0)
            await InsertGoldenManifestDecisionsRelationalAsync(manifest, connection, transaction, ct);
    }
}
