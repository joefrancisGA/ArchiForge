using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Compliance.Evaluators;
using ArchLucid.Decisioning.Compliance.Loaders;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Builders;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Rules;
using ArchLucid.Decisioning.Services;
using ArchLucid.Decisioning.Validation;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.TestSupport.GoldenCorpus;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchLucid.Decisioning.Tests.GoldenCorpus;

/// <summary>
/// Runs the in-process authority decisioning slice used on commit: findings orchestration →
/// <see cref="RuleBasedDecisionEngine.DecideAsync"/> → optional <see cref="DecisionEngineService.MergeResults"/>,
/// mirroring <see cref="ArchLucid.Persistence.Orchestration.Pipeline.AuthorityPipelineStagesExecutor"/> audit for manifest generation.
/// </summary>
public sealed class GoldenCorpusHarness(string complianceRulesPath, TimeProvider timeProvider)
{
    private readonly string _complianceRulesPath =
        complianceRulesPath ?? throw new ArgumentNullException(nameof(complianceRulesPath));

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Runs findings + authority decisioning (+ optional merge) and returns normalized JSON artifacts.</summary>
    public async Task<GoldenCorpusRunArtifacts> RunAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graph,
        CollectingAuditService audit,
        GoldenCorpusMergeInput? merge,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(audit);

        IFindingEngine[] engines = CreateEngines();
        FindingsOrchestrator orchestrator = new(
            engines,
            new FindingPayloadValidator(),
            NullLogger<FindingsOrchestrator>.Instance,
            _timeProvider);

        FindingsSnapshot findings = await orchestrator.GenerateFindingsSnapshotAsync(runId, contextSnapshotId, graph, ct);

        RuleBasedDecisionEngine decisionEngine = new(
            new InMemoryDecisionRuleProvider(),
            new DefaultGoldenManifestBuilder(),
            new GoldenManifestValidator(),
            new ManifestHashService());

        (GoldenManifest manifest, DecisionTrace trace) =
            await decisionEngine.DecideAsync(runId, contextSnapshotId, graph, findings, ct);

        await audit.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ManifestGenerated,
                RunId = runId,
                ManifestId = manifest.ManifestId,
                ActorUserId = "golden-corpus",
                ActorUserName = "golden-corpus",
                TenantId = Guid.Empty,
                WorkspaceId = Guid.Empty,
                ProjectId = Guid.Empty,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        manifest.ManifestHash,
                        manifest.RuleSetId,
                    }),
            },
            ct);

        _ = trace;

        GoldenCorpusMergeSummary? mergeSummary = merge is null ? null : RunMerge(merge);

        return new GoldenCorpusRunArtifacts(
            FindingsJson: GoldenCorpusNormalization.SerializeFindings(findings),
            DecisionsJson: GoldenCorpusNormalization.SerializeDecisions(manifest, mergeSummary),
            AuditTypesJson: GoldenCorpusNormalization.SerializeAuditTypes(audit));
    }

    private IFindingEngine[] CreateEngines()
    {
        GraphCoverageAnalyzer analyzer = new();
        FileComplianceRulePackLoader complianceLoader = new(_complianceRulesPath);
        FileComplianceRulePackProvider complianceProvider = new(complianceLoader);
        ComplianceRulePackValidator complianceValidator = new();
        GraphComplianceEvaluator complianceEvaluator = new();

        return
        [
            new RequirementFindingEngine(),
            new TopologyCoverageFindingEngine(analyzer),
            new SecurityBaselineFindingEngine(),
            new SecurityCoverageFindingEngine(analyzer),
            new ComplianceFindingEngine(complianceProvider, complianceValidator, complianceEvaluator),
            new CostConstraintFindingEngine(),
        ];
    }

    private GoldenCorpusMergeSummary RunMerge(GoldenCorpusMergeInput merge)
    {
        SchemaValidationService validationService = new(
            NullLogger<SchemaValidationService>.Instance,
            Options.Create(new SchemaValidationOptions()));

        DecisionEngineService service = new(validationService);

        DecisionMergeResult result = service.MergeResults(
            merge.MergeRunId,
            merge.Request,
            merge.ManifestVersion,
            merge.AgentResults,
            merge.Evaluations,
            merge.DecisionNodes,
            merge.ParentManifestVersion);

        List<string> errors = result.Errors.OrderBy(static e => e, StringComparer.Ordinal).ToList();
        List<string> serviceIds = result.Manifest is null
            ? []
            : result.Manifest.Services.Select(static s => s.ServiceId).OrderBy(static id => id, StringComparer.Ordinal).ToList();

        return new GoldenCorpusMergeSummary(result.Success, errors, serviceIds);
    }
}

/// <summary>Optional coordinator-style merge payload (same inputs as <see cref="DecisionEngineService.MergeResults"/>).</summary>
public sealed class GoldenCorpusMergeInput
{
    public required string MergeRunId
    {
        get; init;
    }

    public required ArchitectureRequest Request
    {
        get; init;
    }

    public required string ManifestVersion
    {
        get; init;
    }

    public required IReadOnlyList<AgentResult> AgentResults
    {
        get; init;
    }

    public required IReadOnlyList<AgentEvaluation> Evaluations
    {
        get; init;
    }

    public required IReadOnlyList<DecisionNode> DecisionNodes
    {
        get; init;
    }

    public string? ParentManifestVersion
    {
        get; init;
    }
}

public sealed record GoldenCorpusMergeSummary(bool Success, IReadOnlyList<string> Errors, IReadOnlyList<string> ServiceIds);

public sealed record GoldenCorpusRunArtifacts(string FindingsJson, string DecisionsJson, string AuditTypesJson);

/// <summary>Deterministic projections for golden file comparison (sorted keys / lists).</summary>
public static class GoldenCorpusNormalization
{
    private static readonly JsonSerializerOptions WriteOptions = new(GoldenCorpusJson.SerializerOptions)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string SerializeFindings(FindingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<FindingGoldenRow> rows = snapshot.Findings
            .Select(FindingGoldenRow.FromFinding)
            .OrderBy(static r => r.FindingType, StringComparer.Ordinal)
            .ThenBy(static r => r.Title, StringComparer.Ordinal)
            .ToList();

        return JsonSerializer.Serialize(rows, WriteOptions);
    }

    public static string SerializeDecisions(GoldenManifest manifest, GoldenCorpusMergeSummary? merge)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        List<DecisionGoldenRow> authority = manifest.Decisions
            .Select(static d => new DecisionGoldenRow(d.Category, d.SelectedOption, d.Title, d.Rationale))
            .OrderBy(static r => r.Category, StringComparer.Ordinal)
            .ThenBy(static r => r.Title, StringComparer.Ordinal)
            .ToList();

        object payload = new
        {
            authority,
            merge = merge is null
                ? null
                : new
                {
                    merge.Success,
                    merge.Errors,
                    merge.ServiceIds,
                },
        };

        return JsonSerializer.Serialize(payload, WriteOptions);
    }

    public static string SerializeAuditTypes(CollectingAuditService audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        List<string> sorted = audit.EventTypes
            .OrderBy(static t => t, StringComparer.Ordinal)
            .ToList();

        return JsonSerializer.Serialize(sorted, WriteOptions);
    }

    private sealed record FindingGoldenRow(
        string FindingId,
        string FindingType,
        string Category,
        string Severity,
        string Title,
        string Rationale,
        IReadOnlyList<string> RelatedNodeIds,
        string? PayloadType,
        IReadOnlyList<string> RulesApplied,
        IReadOnlyList<string> DecisionsTaken)
    {
        /// <summary>
        /// Production engines assign runtime <see cref="Finding.FindingId"/> values; golden JSON must not depend on those.
        /// This surrogate is stable for the same logical finding across record/replay and CI machines.
        /// </summary>
        private static string StableFindingId(Finding f)
        {
            ArgumentNullException.ThrowIfNull(f);

            string related = string.Join('|', f.RelatedNodeIds.OrderBy(static x => x, StringComparer.Ordinal));
            string rules = string.Join('|', f.Trace.RulesApplied.OrderBy(static x => x, StringComparer.Ordinal));
            string decisions = string.Join('|', f.Trace.DecisionsTaken.OrderBy(static x => x, StringComparer.Ordinal));
            string canonical = string.Join(
                '\n',
                new[]
                {
                    f.FindingType,
                    f.Category,
                    f.Title,
                    f.Rationale,
                    related,
                    f.PayloadType ?? string.Empty,
                    rules,
                    decisions,
                });

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            Span<byte> guidBytes = stackalloc byte[16];
            hash.AsSpan(0, 16).CopyTo(guidBytes);

            return new Guid(guidBytes).ToString("d");
        }

        public static FindingGoldenRow FromFinding(Finding f) => new(
            StableFindingId(f),
            f.FindingType,
            f.Category,
            f.Severity.ToString(),
            f.Title,
            f.Rationale,
            f.RelatedNodeIds.OrderBy(static x => x, StringComparer.Ordinal).ToList(),
            f.PayloadType,
            f.Trace.RulesApplied.OrderBy(static x => x, StringComparer.Ordinal).ToList(),
            f.Trace.DecisionsTaken.OrderBy(static x => x, StringComparer.Ordinal).ToList());
    }

    private sealed record DecisionGoldenRow(string Category, string SelectedOption, string Title, string Rationale);
}
