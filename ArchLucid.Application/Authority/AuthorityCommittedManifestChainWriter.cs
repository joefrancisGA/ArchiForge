using System.Data;

using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Authority;

/// <inheritdoc cref="IAuthorityCommittedManifestChainWriter" />
public sealed class AuthorityCommittedManifestChainWriter(
    IContextSnapshotRepository contextSnapshots,
    IGraphSnapshotRepository graphSnapshots,
    IFindingsSnapshotRepository findingsSnapshots,
    IDecisionTraceRepository decisionTraces,
    IGoldenManifestRepository goldenManifests,
    IManifestHashService manifestHash) : IAuthorityCommittedManifestChainWriter
{
    private const string DemoRuleSetId = "archlucid.authority.demo-seed";
    private const string DemoRuleSetVersion = "1";
    private const string DemoRuleSetHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

    private readonly IContextSnapshotRepository _contextSnapshots =
        contextSnapshots ?? throw new ArgumentNullException(nameof(contextSnapshots));

    private readonly IGraphSnapshotRepository _graphSnapshots =
        graphSnapshots ?? throw new ArgumentNullException(nameof(graphSnapshots));

    private readonly IFindingsSnapshotRepository _findingsSnapshots =
        findingsSnapshots ?? throw new ArgumentNullException(nameof(findingsSnapshots));

    private readonly IDecisionTraceRepository _decisionTraces =
        decisionTraces ?? throw new ArgumentNullException(nameof(decisionTraces));

    private readonly IGoldenManifestRepository _goldenManifests =
        goldenManifests ?? throw new ArgumentNullException(nameof(goldenManifests));

    private readonly IManifestHashService _manifestHash =
        manifestHash ?? throw new ArgumentNullException(nameof(manifestHash));

    /// <inheritdoc />
    public async Task<AuthorityManifestPersistResult> PersistCommittedChainAsync(
        ScopeContext scope,
        Guid authorityRunId,
        string projectSlug,
        Cm.GoldenManifest contract,
        AuthorityChainKeying chainIds,
        DateTime createdUtc,
        bool richFindingsAndGraph,
        CancellationToken cancellationToken,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        if (string.IsNullOrWhiteSpace(projectSlug))
            throw new ArgumentException("Project slug is required.", nameof(projectSlug));

        if (contract is null)
            throw new ArgumentNullException(nameof(contract));

        ContextSnapshot context = BuildContextSnapshot(chainIds.ContextSnapshotId, authorityRunId, projectSlug, createdUtc);
        GraphSnapshot graph = BuildGraphSnapshot(
            chainIds.GraphSnapshotId,
            chainIds.ContextSnapshotId,
            authorityRunId,
            createdUtc,
            richFindingsAndGraph);

        (FindingsSnapshot findings, IReadOnlyList<string> acceptedFindingIds) =
            BuildFindingsSnapshot(
                chainIds.FindingsSnapshotId,
                authorityRunId,
                chainIds.ContextSnapshotId,
                chainIds.GraphSnapshotId,
                createdUtc,
                richFindingsAndGraph);

        RuleAuditTrace ruleAudit = BuildRuleAudit(scope, chainIds.DecisionTraceId, authorityRunId, createdUtc, acceptedFindingIds);

        await _contextSnapshots.SaveAsync(context, cancellationToken, connection, transaction);
        await _graphSnapshots.SaveAsync(graph, cancellationToken, connection, transaction);
        await _findingsSnapshots.SaveAsync(findings, cancellationToken, connection, transaction);
        await _decisionTraces.SaveAsync(ruleAudit, cancellationToken, connection, transaction);

        SaveContractsManifestOptions keying = new()
        {
            ManifestId = chainIds.ManifestId,
            RunId = authorityRunId,
            ContextSnapshotId = chainIds.ContextSnapshotId,
            GraphSnapshotId = chainIds.GraphSnapshotId,
            FindingsSnapshotId = chainIds.FindingsSnapshotId,
            DecisionTraceId = chainIds.DecisionTraceId,
            RuleSetId = DemoRuleSetId,
            RuleSetVersion = DemoRuleSetVersion,
            RuleSetHash = DemoRuleSetHash,
            CreatedUtc = createdUtc,
        };

        await _goldenManifests.SaveAsync(
            contract,
            scope,
            keying,
            _manifestHash,
            cancellationToken,
            connection,
            transaction,
            authorityPersistBody: null);

        return new AuthorityManifestPersistResult(
            chainIds.ContextSnapshotId,
            chainIds.GraphSnapshotId,
            chainIds.FindingsSnapshotId,
            chainIds.DecisionTraceId,
            chainIds.ManifestId);
    }

    private static ContextSnapshot BuildContextSnapshot(
        Guid snapshotId,
        Guid runId,
        string projectSlug,
        DateTime createdUtc)
    {
        return new ContextSnapshot
        {
            SnapshotId = snapshotId,
            RunId = runId,
            ProjectId = projectSlug,
            CreatedUtc = createdUtc,
            CanonicalObjects =
            [
                new CanonicalObject
                {
                    ObjectType = "system",
                    Name = projectSlug,
                    SourceType = "authority-seed",
                    SourceId = runId.ToString("N"),
                },
            ],
            Warnings = [],
            Errors = [],
            SourceHashes = new Dictionary<string, string> { ["demo"] = "1" },
        };
    }

    private static GraphSnapshot BuildGraphSnapshot(
        Guid graphSnapshotId,
        Guid contextSnapshotId,
        Guid runId,
        DateTime createdUtc,
        bool rich)
    {
        GraphSnapshot graph = new()
        {
            GraphSnapshotId = graphSnapshotId,
            ContextSnapshotId = contextSnapshotId,
            RunId = runId,
            CreatedUtc = createdUtc,
            Warnings = [],
        };

        if (!rich)
            return graph;

        graph.Nodes.Add(
            new GraphNode
            {
                NodeId = "node-checkout-api",
                NodeType = "service",
                Label = "Checkout API",
                Category = "compute",
                SourceType = "demo",
                SourceId = "seed",
            });

        graph.Nodes.Add(
            new GraphNode
            {
                NodeId = "node-orders-db",
                NodeType = "datastore",
                Label = "Orders DB",
                Category = "data",
                SourceType = "demo",
                SourceId = "seed",
            });

        graph.Edges.Add(
            new GraphEdge
            {
                EdgeId = "edge-checkout-to-db",
                FromNodeId = "node-checkout-api",
                ToNodeId = "node-orders-db",
                EdgeType = "dependsOn",
                Weight = 1d,
            });

        return graph;
    }

    private static (FindingsSnapshot Snapshot, IReadOnlyList<string> AcceptedIds) BuildFindingsSnapshot(
        Guid findingsSnapshotId,
        Guid runId,
        Guid contextSnapshotId,
        Guid graphSnapshotId,
        DateTime createdUtc,
        bool rich)
    {
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = findingsSnapshotId,
            RunId = runId,
            ContextSnapshotId = contextSnapshotId,
            GraphSnapshotId = graphSnapshotId,
            CreatedUtc = createdUtc,
            EngineFailures = [],
        };

        Finding primary = new()
        {
            FindingId = $"finding-demo-{runId:N}-primary",
            FindingType = "ArchitectureReview",
            Category = "Cost",
            EngineType = "DemoSeed",
            Severity = FindingSeverity.Warning,
            Title = "Demo finding — cost posture",
            Rationale = "Seeded finding so authority decision trace accepts at least one finding id.",
        };

        snapshot.Findings.Add(primary);

        if (rich)
        {
            snapshot.Findings.Add(
                new Finding
                {
                    FindingId = $"finding-demo-{runId:N}-secondary",
                    FindingType = "ComplianceReview",
                    Category = "Security",
                    EngineType = "DemoSeed",
                    Severity = FindingSeverity.Info,
                    Title = "Demo finding — security control coverage",
                    Rationale = "Secondary seeded finding for vertical-style demo density.",
                });
        }

        List<string> accepted = snapshot.Findings.ConvertAll(f => f.FindingId);
        return (snapshot, accepted);
    }

    private static RuleAuditTrace BuildRuleAudit(
        ScopeContext scope,
        Guid decisionTraceId,
        Guid runId,
        DateTime createdUtc,
        IReadOnlyList<string> acceptedFindingIds)
    {
        RuleAuditTracePayload payload = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            DecisionTraceId = decisionTraceId,
            RunId = runId,
            CreatedUtc = createdUtc,
            RuleSetId = DemoRuleSetId,
            RuleSetVersion = DemoRuleSetVersion,
            RuleSetHash = DemoRuleSetHash,
            AppliedRuleIds = ["demo-seed-rule"],
            AcceptedFindingIds = [.. acceptedFindingIds],
            RejectedFindingIds = [],
            Notes = ["Seeded authority rule-audit trace (demo / replay FK chain)."],
        };

        return RuleAuditTrace.From(payload);
    }
}
