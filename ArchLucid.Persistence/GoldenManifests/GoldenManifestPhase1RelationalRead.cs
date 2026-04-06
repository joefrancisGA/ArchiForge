using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.RelationalRead;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.GoldenManifests;

/// <summary>Phase-1 relational-first hydration for <see cref="GoldenManifest"/>; JSON fallback governed by <see cref="JsonFallbackPolicy"/>.</summary>
internal static class GoldenManifestPhase1RelationalRead
{
    internal static async Task<GoldenManifest> HydrateAsync(
        SqlConnection connection,
        GoldenManifestStorageRow row,
        CancellationToken ct,
        JsonFallbackPolicy? fallbackPolicy = null)
    {
        Guid manifestId = row.ManifestId;

        int assumptionsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.GoldenManifestAssumptions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int warningsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.GoldenManifestWarnings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int decisionsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.GoldenManifestDecisions WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provFindingCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceFindings WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provNodeCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceGraphNodes WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        int provRuleCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction: null,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceAppliedRules WHERE ManifestId = @ManifestId",
            new
            {
                ManifestId = manifestId,
            },
            ct);

        string entityId = manifestId.ToString();

        List<string> assumptions = await RelationalFirstRead.ReadSliceAsync(
            assumptionsCount,
            "GoldenManifest.Assumptions",
            () => LoadOrderedStringsAsync(
                connection,
                """
                SELECT AssumptionText AS Item
                FROM dbo.GoldenManifestAssumptions
                WHERE ManifestId = @ManifestId
                ORDER BY SortOrder;
                """,
                manifestId,
                ct),
            () => GoldenManifestJsonFallback.DeserializeStringList(row.AssumptionsJson),
            () => [],
            fallbackPolicy,
            "GoldenManifest", entityId);

        List<string> warnings = await RelationalFirstRead.ReadSliceAsync(
            warningsCount,
            "GoldenManifest.Warnings",
            () => LoadOrderedStringsAsync(
                connection,
                """
                SELECT WarningText AS Item
                FROM dbo.GoldenManifestWarnings
                WHERE ManifestId = @ManifestId
                ORDER BY SortOrder;
                """,
                manifestId,
                ct),
            () => GoldenManifestJsonFallback.DeserializeStringList(row.WarningsJson),
            () => [],
            fallbackPolicy,
            "GoldenManifest", entityId);

        int totalProvCount = provFindingCount + provNodeCount + provRuleCount;
        ManifestProvenance provenance;

        if (totalProvCount > 0)
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
        else if (fallbackPolicy is null || fallbackPolicy.EvaluateFallback(totalProvCount, "GoldenManifest.Provenance", "GoldenManifest", entityId))
        
            provenance = GoldenManifestJsonFallback.DeserializeProvenance(row.ProvenanceJson);
        
        else
        
            provenance = new ManifestProvenance();
        

        List<ResolvedArchitectureDecision> decisions = await RelationalFirstRead.ReadSliceAsync(
            decisionsCount,
            "GoldenManifest.Decisions",
            () => LoadDecisionsRelationalAsync(connection, manifestId, ct),
            () => GoldenManifestJsonFallback.DeserializeDecisions(row.DecisionsJson),
            () => [],
            fallbackPolicy,
            "GoldenManifest", entityId);

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

    private static ComplianceSection DeserializeCompliance(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? new ComplianceSection() : JsonEntitySerializer.Deserialize<ComplianceSection>(json);
    }

    private sealed class ManifestDecisionRow
    {
        public int SortOrder { get; init; }
        public string DecisionId { get; init; } = null!;
        public string Category { get; init; } = null!;
        public string Title { get; init; } = null!;
        public string SelectedOption { get; init; } = null!;
        public string Rationale { get; init; } = null!;
        public string? RawDecisionJson { get; init; }
    }

    private sealed class DecisionEvidenceRow
    {
        public string DecisionId { get; init; } = null!;
        public int SortOrder { get; init; }
        public string FindingId { get; init; } = null!;
    }

    private sealed class DecisionNodeRow
    {
        public string DecisionId { get; init; } = null!;
        public int SortOrder { get; init; }
        public string NodeId { get; init; } = null!;
    }
}
