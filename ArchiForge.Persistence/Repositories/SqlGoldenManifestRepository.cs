using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
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
        using SqlTransaction tx = owned.BeginTransaction();

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
        GoldenManifestRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestRow>(
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

        return await HydrateAsync(connection, row, ct);
    }

    private static async Task<GoldenManifest> HydrateAsync(
        SqlConnection connection,
        GoldenManifestRow row,
        CancellationToken ct)
    {
        Guid manifestId = row.ManifestId;

        int assumptionsCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifestAssumptions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int warningsCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifestWarnings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int decisionsCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifestDecisions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provFindingCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceFindings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provNodeCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceGraphNodes WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provRuleCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceAppliedRules WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        List<string> assumptions = assumptionsCount > 0
            ? await LoadOrderedStringsAsync(
                connection,
                """
                SELECT AssumptionText AS Item
                FROM dbo.GoldenManifestAssumptions
                WHERE ManifestId = @ManifestId
                ORDER BY SortOrder;
                """,
                manifestId,
                ct)
            : JsonEntitySerializer.Deserialize<List<string>>(row.AssumptionsJson);

        List<string> warnings = warningsCount > 0
            ? await LoadOrderedStringsAsync(
                connection,
                """
                SELECT WarningText AS Item
                FROM dbo.GoldenManifestWarnings
                WHERE ManifestId = @ManifestId
                ORDER BY SortOrder;
                """,
                manifestId,
                ct)
            : JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson);

        ManifestProvenance provenance;
        if (provFindingCount + provNodeCount + provRuleCount > 0)
        {
            List<string> sourceFindings = provFindingCount > 0
                ? await LoadOrderedStringsAsync(
                    connection,
                    """
                    SELECT FindingId AS Item
                    FROM dbo.GoldenManifestProvenanceSourceFindings
                    WHERE ManifestId = @ManifestId
                    ORDER BY SortOrder;
                    """,
                    manifestId,
                    ct)
                : [];

            List<string> sourceNodes = provNodeCount > 0
                ? await LoadOrderedStringsAsync(
                    connection,
                    """
                    SELECT NodeId AS Item
                    FROM dbo.GoldenManifestProvenanceSourceGraphNodes
                    WHERE ManifestId = @ManifestId
                    ORDER BY SortOrder;
                    """,
                    manifestId,
                    ct)
                : [];

            List<string> appliedRules = provRuleCount > 0
                ? await LoadOrderedStringsAsync(
                    connection,
                    """
                    SELECT RuleId AS Item
                    FROM dbo.GoldenManifestProvenanceAppliedRules
                    WHERE ManifestId = @ManifestId
                    ORDER BY SortOrder;
                    """,
                    manifestId,
                    ct)
                : [];

            provenance = new ManifestProvenance
            {
                SourceFindingIds = sourceFindings,
                SourceGraphNodeIds = sourceNodes,
                AppliedRuleIds = appliedRules,
            };
        }
        else
        {
            provenance = JsonEntitySerializer.Deserialize<ManifestProvenance>(row.ProvenanceJson);
        }

        List<ResolvedArchitectureDecision> decisions = decisionsCount > 0
            ? await LoadDecisionsRelationalAsync(connection, manifestId, ct)
            : JsonEntitySerializer.Deserialize<List<ResolvedArchitectureDecision>>(row.DecisionsJson);

        return new GoldenManifest
        {
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            ManifestId = row.ManifestId,
            RunId = row.RunId,
            ContextSnapshotId = row.ContextSnapshotId,
            GraphSnapshotId = row.GraphSnapshotId,
            FindingsSnapshotId = row.FindingsSnapshotId,
            DecisionTraceId = row.DecisionTraceId,
            CreatedUtc = row.CreatedUtc,
            ManifestHash = row.ManifestHash,
            RuleSetId = row.RuleSetId,
            RuleSetVersion = row.RuleSetVersion,
            RuleSetHash = row.RuleSetHash,
            Metadata = JsonEntitySerializer.Deserialize<ManifestMetadata>(row.MetadataJson),
            Requirements = JsonEntitySerializer.Deserialize<RequirementsCoverageSection>(row.RequirementsJson),
            Topology = JsonEntitySerializer.Deserialize<TopologySection>(row.TopologyJson),
            Security = JsonEntitySerializer.Deserialize<SecuritySection>(row.SecurityJson),
            Compliance = DeserializeCompliance(row.ComplianceJson),
            Cost = JsonEntitySerializer.Deserialize<CostSection>(row.CostJson),
            Constraints = JsonEntitySerializer.Deserialize<ConstraintSection>(row.ConstraintsJson),
            UnresolvedIssues = JsonEntitySerializer.Deserialize<UnresolvedIssuesSection>(row.UnresolvedIssuesJson),
            Decisions = decisions,
            Assumptions = assumptions,
            Warnings = warnings,
            Provenance = provenance,
        };
    }

    private static async Task<List<ResolvedArchitectureDecision>> LoadDecisionsRelationalAsync(
        SqlConnection connection,
        Guid manifestId,
        CancellationToken ct)
    {
        const string decisionsSql = """
            SELECT SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson
            FROM dbo.GoldenManifestDecisions
            WHERE ManifestId = @ManifestId
            ORDER BY SortOrder;
            """;

        List<ManifestDecisionRow> decisionRows = (await connection.QueryAsync<ManifestDecisionRow>(
            new CommandDefinition(
                decisionsSql,
                new
                {
                    ManifestId = manifestId,
                },
                cancellationToken: ct))).ToList();

        if (decisionRows.Count == 0)
            return [];

        const string evidenceSql = """
            SELECT DecisionId, SortOrder, FindingId
            FROM dbo.GoldenManifestDecisionEvidenceLinks
            WHERE ManifestId = @ManifestId
            ORDER BY DecisionId, SortOrder;
            """;

        List<DecisionEvidenceRow> evidenceRows = (await connection.QueryAsync<DecisionEvidenceRow>(
            new CommandDefinition(
                evidenceSql,
                new
                {
                    ManifestId = manifestId,
                },
                cancellationToken: ct))).ToList();

        const string nodeSql = """
            SELECT DecisionId, SortOrder, NodeId
            FROM dbo.GoldenManifestDecisionNodeLinks
            WHERE ManifestId = @ManifestId
            ORDER BY DecisionId, SortOrder;
            """;

        List<DecisionNodeRow> nodeRows = (await connection.QueryAsync<DecisionNodeRow>(
            new CommandDefinition(
                nodeSql,
                new
                {
                    ManifestId = manifestId,
                },
                cancellationToken: ct))).ToList();

        Dictionary<string, List<string>> evidenceByDecision = new(StringComparer.Ordinal);
        foreach (DecisionEvidenceRow er in evidenceRows)
        {
            if (!evidenceByDecision.TryGetValue(er.DecisionId, out List<string>? list))
            {
                list = [];
                evidenceByDecision[er.DecisionId] = list;
            }

            list.Add(er.FindingId);
        }

        Dictionary<string, List<string>> nodesByDecision = new(StringComparer.Ordinal);
        foreach (DecisionNodeRow nr in nodeRows)
        {
            if (!nodesByDecision.TryGetValue(nr.DecisionId, out List<string>? list))
            {
                list = [];
                nodesByDecision[nr.DecisionId] = list;
            }

            list.Add(nr.NodeId);
        }

        List<ResolvedArchitectureDecision> result = [];
        foreach (ManifestDecisionRow dr in decisionRows)
        {
            evidenceByDecision.TryGetValue(dr.DecisionId, out List<string>? ev);
            ev ??= [];

            nodesByDecision.TryGetValue(dr.DecisionId, out List<string>? nodes);
            nodes ??= [];

            result.Add(
                new ResolvedArchitectureDecision
                {
                    DecisionId = dr.DecisionId,
                    Category = dr.Category,
                    Title = dr.Title,
                    SelectedOption = dr.SelectedOption,
                    Rationale = dr.Rationale,
                    SupportingFindingIds = ev,
                    RelatedNodeIds = nodes,
                    RawDecisionJson = dr.RawDecisionJson,
                });
        }

        return result;
    }

    private static async Task<List<string>> LoadOrderedStringsAsync(
        SqlConnection connection,
        string sql,
        Guid manifestId,
        CancellationToken ct)
    {
        IEnumerable<string> rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new
                {
                    ManifestId = manifestId,
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    private static async Task<int> ScalarCountAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction, cancellationToken: ct));
        return count;
    }

    private static async Task<int> ScalarCountAsync(
        SqlConnection connection,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, cancellationToken: ct));
        return count;
    }

    private static ComplianceSection DeserializeCompliance(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? new ComplianceSection() : JsonEntitySerializer.Deserialize<ComplianceSection>(json);
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

        int assumptionsCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestAssumptions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int warningsCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestWarnings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provFindingCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceFindings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provNodeCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceGraphNodes WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provRuleCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceAppliedRules WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int decisionsCount = await ScalarCountAsync(
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

    private sealed class GoldenManifestRow
    {
        public Guid TenantId
        {
            get; init;
        }

        public Guid WorkspaceId
        {
            get; init;
        }

        public Guid ProjectId
        {
            get; init;
        }

        public Guid ManifestId
        {
            get; init;
        }

        public Guid RunId
        {
            get; init;
        }

        public Guid ContextSnapshotId
        {
            get; init;
        }

        public Guid GraphSnapshotId
        {
            get; init;
        }

        public Guid FindingsSnapshotId
        {
            get; init;
        }

        public Guid DecisionTraceId
        {
            get; init;
        }

        public DateTime CreatedUtc
        {
            get; init;
        }

        public string ManifestHash { get; init; } = null!;

        public string RuleSetId { get; init; } = null!;

        public string RuleSetVersion { get; init; } = null!;

        public string RuleSetHash { get; init; } = null!;

        public string MetadataJson { get; init; } = null!;

        public string RequirementsJson { get; init; } = null!;

        public string TopologyJson { get; init; } = null!;

        public string SecurityJson { get; init; } = null!;

        public string? ComplianceJson
        {
            get; init;
        }

        public string CostJson { get; init; } = null!;

        public string ConstraintsJson { get; init; } = null!;

        public string UnresolvedIssuesJson { get; init; } = null!;

        public string DecisionsJson { get; init; } = null!;

        public string AssumptionsJson { get; init; } = null!;

        public string WarningsJson { get; init; } = null!;

        public string ProvenanceJson { get; init; } = null!;
    }

    private sealed class ManifestDecisionRow
    {
        public int SortOrder
        {
            get; init;
        }

        public string DecisionId { get; init; } = null!;

        public string Category { get; init; } = null!;

        public string Title { get; init; } = null!;

        public string SelectedOption { get; init; } = null!;

        public string Rationale { get; init; } = null!;

        public string? RawDecisionJson
        {
            get; init;
        }
    }

    private sealed class DecisionEvidenceRow
    {
        public string DecisionId { get; init; } = null!;

        public int SortOrder
        {
            get; init;
        }

        public string FindingId { get; init; } = null!;
    }

    private sealed class DecisionNodeRow
    {
        public string DecisionId { get; init; } = null!;

        public int SortOrder
        {
            get; init;
        }

        public string NodeId { get; init; } = null!;
    }
}
