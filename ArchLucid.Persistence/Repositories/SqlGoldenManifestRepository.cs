using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Mapping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GoldenManifests;
using ArchLucid.Persistence.RelationalRead;
using ArchLucid.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     SQL Server-backed <see cref="IGoldenManifestRepository" /> with dual-write to legacy JSON columns and
///     phase-1 relational tables for assumptions, warnings, decisions (+ evidence/node links + RawDecisionJson),
///     and provenance reference lists. Reads prefer relational slices per collection when rows exist.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlGoldenManifestRepository(
    ISqlConnectionFactory connectionFactory,
    IGoldenManifestLookupReadConnectionFactory manifestLookupReadConnectionFactory,
    IArtifactBlobStore blobStore,
    IOptionsMonitor<ArtifactLargePayloadOptions> largePayloadOptions) : IGoldenManifestRepository
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

    public async Task<GoldenManifest> SaveAsync(
        Cm.GoldenManifest contract,
        ScopeContext scope,
        SaveContractsManifestOptions keying,
        IManifestHashService contractHash,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        GoldenManifest? authorityPersistBody = null)
    {
        if (contract is null)
            throw new ArgumentNullException(nameof(contract));
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));
        if (keying is null)
            throw new ArgumentNullException(nameof(keying));
        if (contractHash is null)
            throw new ArgumentNullException(nameof(contractHash));
        GoldenManifest model = ContractGoldenManifestPersistence.ResolveGoldenManifestForContractSave(
            contract,
            scope,
            keying,
            authorityPersistBody);
        model.ManifestHash = contractHash.ComputeHash(model);
        await SaveAsync(model, ct, connection, transaction);
        return model;
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
                               WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                           FROM dbo.GoldenManifests
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId
                             AND ManifestId = @ManifestId;
                           """;

        await using SqlConnection connection = await manifestLookupReadConnectionFactory.CreateOpenConnectionAsync(ct);
        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(
                sql,
                new { scope.TenantId, scope.WorkspaceId, scope.ProjectId, ManifestId = manifestId },
                cancellationToken: ct));

        if (row is null)
            return null;

        row = await ApplyManifestBlobOverlayIfPresentAsync(row, ct);

        return await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, ct);
    }

    /// <inheritdoc />
    public async Task<GoldenManifest?> GetByContractManifestVersionAsync(
        ScopeContext scope,
        string manifestVersion,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (string.IsNullOrWhiteSpace(manifestVersion))
            throw new ArgumentException("Manifest version is required.", nameof(manifestVersion));

        const string sql = """
                           SELECT TOP (1)
                               TenantId, WorkspaceId, ProjectId,
                               ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                               CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                               MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                               ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                               WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                           FROM dbo.GoldenManifests
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId
                             AND JSON_VALUE(MetadataJson, '$.Version') = @ManifestVersion
                           ORDER BY CreatedUtc DESC;
                           """;

        await using SqlConnection connection = await manifestLookupReadConnectionFactory.CreateOpenConnectionAsync(ct);
        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(
                sql,
                new { scope.TenantId, scope.WorkspaceId, scope.ProjectId, ManifestVersion = manifestVersion },
                cancellationToken: ct));

        if (row is null)
            return null;

        row = await ApplyManifestBlobOverlayIfPresentAsync(row, ct);

        return await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, ct);
    }

    private async Task SaveCoreAsync(
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
                               WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                           )
                           VALUES
                           (
                               @TenantId, @WorkspaceId, @ProjectId,
                               @ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                               @CreatedUtc, @ManifestHash, @RuleSetId, @RuleSetVersion, @RuleSetHash,
                               @MetadataJson, @RequirementsJson, @TopologyJson, @SecurityJson, @ComplianceJson, @CostJson,
                               @ConstraintsJson, @UnresolvedIssuesJson, @DecisionsJson, @AssumptionsJson,
                               @WarningsJson, @ProvenanceJson, @ManifestPayloadBlobUri
                           );
                           """;

        string metadataJson = JsonEntitySerializer.Serialize(manifest.Metadata);
        string requirementsJson = JsonEntitySerializer.Serialize(manifest.Requirements);
        string topologyJson = JsonEntitySerializer.Serialize(manifest.Topology);
        string securityJson = JsonEntitySerializer.Serialize(manifest.Security);
        string complianceJson = JsonEntitySerializer.Serialize(manifest.Compliance);
        string costJson = JsonEntitySerializer.Serialize(manifest.Cost);
        string constraintsJson = JsonEntitySerializer.Serialize(manifest.Constraints);
        string unresolvedIssuesJson = JsonEntitySerializer.Serialize(manifest.UnresolvedIssues);
        string decisionsJson = JsonEntitySerializer.Serialize(manifest.Decisions);
        string assumptionsJson = JsonEntitySerializer.Serialize(manifest.Assumptions);
        string warningsJson = JsonEntitySerializer.Serialize(manifest.Warnings);
        string provenanceJson = JsonEntitySerializer.Serialize(manifest.Provenance);

        int totalLen = GoldenManifestPayloadBlobEnvelope.SumUtf16Length(
            metadataJson,
            requirementsJson,
            topologyJson,
            securityJson,
            complianceJson,
            costJson,
            constraintsJson,
            unresolvedIssuesJson,
            decisionsJson,
            assumptionsJson,
            warningsJson,
            provenanceJson);

        ArtifactLargePayloadOptions payloadOpts = largePayloadOptions.CurrentValue;
        string? manifestBlobUri = null;

        if (LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(payloadOpts, totalLen))
        {
            GoldenManifestPayloadBlobEnvelope envelope = GoldenManifestPayloadBlobEnvelope.FromSerializedSlices(
                metadataJson,
                requirementsJson,
                topologyJson,
                securityJson,
                complianceJson,
                costJson,
                constraintsJson,
                unresolvedIssuesJson,
                decisionsJson,
                assumptionsJson,
                warningsJson,
                provenanceJson);
            manifestBlobUri = await blobStore.WriteAsync(
                "golden-manifests",
                $"{manifest.ManifestId:D}.json",
                envelope.ToJson(),
                ct);
        }

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
            MetadataJson = metadataJson,
            RequirementsJson = requirementsJson,
            TopologyJson = topologyJson,
            SecurityJson = securityJson,
            ComplianceJson = complianceJson,
            CostJson = costJson,
            ConstraintsJson = constraintsJson,
            UnresolvedIssuesJson = unresolvedIssuesJson,
            DecisionsJson = decisionsJson,
            AssumptionsJson = assumptionsJson,
            WarningsJson = warningsJson,
            ProvenanceJson = provenanceJson,
            ManifestPayloadBlobUri = manifestBlobUri
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
                                           INSERT INTO dbo.GoldenManifestAssumptions (
                                               ManifestId, SortOrder, AssumptionText,
                                               TenantId, WorkspaceId, ProjectId)
                                           VALUES (
                                               @ManifestId, @SortOrder, @AssumptionText,
                                               @TenantId, @WorkspaceId, @ProjectId);
                                           """;

        for (int i = 0; i < manifest.Assumptions.Count; i++)

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertAssumptionSql,
                    new
                    {
                        ManifestId = manifestId,
                        SortOrder = i,
                        AssumptionText = manifest.Assumptions[i],
                        manifest.TenantId,
                        manifest.WorkspaceId,
                        manifest.ProjectId
                    },
                    transaction,
                    cancellationToken: ct));
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

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertWarningSql,
                    new { ManifestId = manifestId, SortOrder = w, WarningText = manifest.Warnings[w] },
                    transaction,
                    cancellationToken: ct));
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

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertProvFindingSql,
                    new { ManifestId = manifestId, SortOrder = p, FindingId = provFindingIds[p] },
                    transaction,
                    cancellationToken: ct));
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

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertProvNodeSql,
                    new { ManifestId = manifestId, SortOrder = p, NodeId = provNodeIds[p] },
                    transaction,
                    cancellationToken: ct));
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

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertProvRuleSql,
                    new { ManifestId = manifestId, SortOrder = p, RuleId = provRuleIds[p] },
                    transaction,
                    cancellationToken: ct));
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
                        decision.RawDecisionJson
                    },
                    transaction,
                    cancellationToken: ct));

            for (int e = 0; e < decision.SupportingFindingIds.Count; e++)

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertEvidenceSql,
                        new
                        {
                            ManifestId = manifestId,
                            decision.DecisionId,
                            SortOrder = e,
                            FindingId = decision.SupportingFindingIds[e]
                        },
                        transaction,
                        cancellationToken: ct));


            for (int n = 0; n < decision.RelatedNodeIds.Count; n++)

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertNodeLinkSql,
                        new
                        {
                            ManifestId = manifestId,
                            decision.DecisionId,
                            SortOrder = n,
                            NodeId = decision.RelatedNodeIds[n]
                        },
                        transaction,
                        cancellationToken: ct));
        }
    }

    private async Task<GoldenManifestStorageRow> ApplyManifestBlobOverlayIfPresentAsync(
        GoldenManifestStorageRow row,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(row.ManifestPayloadBlobUri))
            return row;

        string? json = await blobStore.ReadAsync(row.ManifestPayloadBlobUri!, ct);

        if (string.IsNullOrEmpty(json))
            return row;

        GoldenManifestPayloadBlobEnvelope? envelope = GoldenManifestPayloadBlobEnvelope.TryDeserialize(json);

        if (envelope is null || envelope.SchemaVersion != GoldenManifestPayloadBlobEnvelope.CurrentSchemaVersion)
            return row;

        return GoldenManifestPayloadBlobEnvelope.MergeIntoRow(row, envelope);
    }

    /// <summary>
    ///     Inserts phase-1 relational slices that are still empty while JSON columns contain data (idempotent per slice).
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
            new { ManifestId = manifestId },
            ct);

        int warningsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestWarnings WHERE ManifestId = @ManifestId",
            new { ManifestId = manifestId },
            ct);

        int provFindingCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceFindings WHERE ManifestId = @ManifestId",
            new { ManifestId = manifestId },
            ct);

        int provNodeCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceSourceGraphNodes WHERE ManifestId = @ManifestId",
            new { ManifestId = manifestId },
            ct);

        int provRuleCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestProvenanceAppliedRules WHERE ManifestId = @ManifestId",
            new { ManifestId = manifestId },
            ct);

        int decisionsCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.GoldenManifestDecisions WHERE ManifestId = @ManifestId",
            new { ManifestId = manifestId },
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
