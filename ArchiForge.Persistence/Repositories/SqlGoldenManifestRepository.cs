using System.Data;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlGoldenManifestRepository : IGoldenManifestRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlGoldenManifestRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        GoldenManifest manifest,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.GoldenManifests
            (
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                WarningsJson, ProvenanceJson
            )
            VALUES
            (
                @ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                @CreatedUtc, @ManifestHash, @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @MetadataJson, @RequirementsJson, @TopologyJson, @SecurityJson, @CostJson,
                @ConstraintsJson, @UnresolvedIssuesJson, @DecisionsJson, @AssumptionsJson,
                @WarningsJson, @ProvenanceJson
            );
            """;

        var args = new
        {
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

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<GoldenManifest?> GetByIdAsync(Guid manifestId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                WarningsJson, ProvenanceJson
            FROM dbo.GoldenManifests
            WHERE ManifestId = @ManifestId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<GoldenManifestRow>(
            new CommandDefinition(sql, new { ManifestId = manifestId }, cancellationToken: ct));

        if (row is null)
            return null;

        return new GoldenManifest
        {
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
            Cost = JsonEntitySerializer.Deserialize<CostSection>(row.CostJson),
            Constraints = JsonEntitySerializer.Deserialize<ConstraintSection>(row.ConstraintsJson),
            UnresolvedIssues = JsonEntitySerializer.Deserialize<UnresolvedIssuesSection>(row.UnresolvedIssuesJson),
            Decisions = JsonEntitySerializer.Deserialize<List<ResolvedArchitectureDecision>>(row.DecisionsJson),
            Assumptions = JsonEntitySerializer.Deserialize<List<string>>(row.AssumptionsJson),
            Warnings = JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson),
            Provenance = JsonEntitySerializer.Deserialize<ManifestProvenance>(row.ProvenanceJson)
        };
    }

    private sealed class GoldenManifestRow
    {
        public Guid ManifestId { get; set; }
        public Guid RunId { get; set; }
        public Guid ContextSnapshotId { get; set; }
        public Guid GraphSnapshotId { get; set; }
        public Guid FindingsSnapshotId { get; set; }
        public Guid DecisionTraceId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string ManifestHash { get; set; } = default!;
        public string RuleSetId { get; set; } = default!;
        public string RuleSetVersion { get; set; } = default!;
        public string RuleSetHash { get; set; } = default!;
        public string MetadataJson { get; set; } = default!;
        public string RequirementsJson { get; set; } = default!;
        public string TopologyJson { get; set; } = default!;
        public string SecurityJson { get; set; } = default!;
        public string CostJson { get; set; } = default!;
        public string ConstraintsJson { get; set; } = default!;
        public string UnresolvedIssuesJson { get; set; } = default!;
        public string DecisionsJson { get; set; } = default!;
        public string AssumptionsJson { get; set; } = default!;
        public string WarningsJson { get; set; } = default!;
        public string ProvenanceJson { get; set; } = default!;
    }
}
