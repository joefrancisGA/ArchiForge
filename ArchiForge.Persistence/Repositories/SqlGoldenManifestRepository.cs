using System.Data;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlGoldenManifestRepository(ISqlConnectionFactory connectionFactory) : IGoldenManifestRepository
{
    public async Task SaveAsync(
        GoldenManifest manifest,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
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

        var args = new
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
            ProvenanceJson = JsonEntitySerializer.Serialize(manifest.Provenance)
        };

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<GoldenManifest?> GetByIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
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

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<GoldenManifestRow>(
            new CommandDefinition(
                sql,
                new
                {
                    scope.TenantId,
                    scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    ManifestId = manifestId
                },
                cancellationToken: ct));

        if (row is null)
            return null;

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
            Decisions = JsonEntitySerializer.Deserialize<List<ResolvedArchitectureDecision>>(row.DecisionsJson),
            Assumptions = JsonEntitySerializer.Deserialize<List<string>>(row.AssumptionsJson),
            Warnings = JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson),
            Provenance = JsonEntitySerializer.Deserialize<ManifestProvenance>(row.ProvenanceJson)
        };
    }

    private static ComplianceSection DeserializeCompliance(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ComplianceSection();
        return JsonEntitySerializer.Deserialize<ComplianceSection>(json);
    }

    private sealed class GoldenManifestRow
    {
        public Guid TenantId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid ManifestId { get; set; }
        public Guid RunId { get; set; }
        public Guid ContextSnapshotId { get; set; }
        public Guid GraphSnapshotId { get; set; }
        public Guid FindingsSnapshotId { get; set; }
        public Guid DecisionTraceId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string ManifestHash { get; set; } = null!;
        public string RuleSetId { get; set; } = null!;
        public string RuleSetVersion { get; set; } = null!;
        public string RuleSetHash { get; set; } = null!;
        public string MetadataJson { get; set; } = null!;
        public string RequirementsJson { get; set; } = null!;
        public string TopologyJson { get; set; } = null!;
        public string SecurityJson { get; set; } = null!;
        public string? ComplianceJson { get; set; }
        public string CostJson { get; set; } = null!;
        public string ConstraintsJson { get; set; } = null!;
        public string UnresolvedIssuesJson { get; set; } = null!;
        public string DecisionsJson { get; set; } = null!;
        public string AssumptionsJson { get; set; } = null!;
        public string WarningsJson { get; set; } = null!;
        public string ProvenanceJson { get; set; } = null!;
    }
}
