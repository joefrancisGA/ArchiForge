using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Validation;

namespace ArchLucid.Decisioning.Merge;

public sealed class DecisionEngineService : IDecisionEngineService
{
    private readonly AgentProposalManifestMerger _agentProposalManifestMerger;
    private readonly DecisionNodeManifestMerger _decisionNodeManifestMerger;
    private readonly ManifestGovernanceMerger _manifestGovernanceMerger;
    private readonly DecisionMergeInputGate _mergeInputGate;
    private readonly ISchemaValidationService _schemaValidationService;

    /// <summary>
    ///     Preferred constructor for DI: merge steps are explicit dependencies for testing and extension.
    /// </summary>
    public DecisionEngineService(
        ISchemaValidationService schemaValidationService,
        DecisionMergeInputGate mergeInputGate,
        AgentProposalManifestMerger agentProposalManifestMerger,
        DecisionNodeManifestMerger decisionNodeManifestMerger,
        ManifestGovernanceMerger manifestGovernanceMerger)
    {
        _schemaValidationService =
            schemaValidationService ?? throw new ArgumentNullException(nameof(schemaValidationService));
        _mergeInputGate = mergeInputGate ?? throw new ArgumentNullException(nameof(mergeInputGate));
        _agentProposalManifestMerger = agentProposalManifestMerger ??
                                       throw new ArgumentNullException(nameof(agentProposalManifestMerger));
        _decisionNodeManifestMerger = decisionNodeManifestMerger ??
                                      throw new ArgumentNullException(nameof(decisionNodeManifestMerger));
        _manifestGovernanceMerger = manifestGovernanceMerger ??
                                    throw new ArgumentNullException(nameof(manifestGovernanceMerger));
    }

    /// <summary>
    ///     Convenience constructor for tests and tools: wires the default merge strategies (same behavior as full DI).
    ///     Host composition registers the five-argument constructor when merge strategies are in the container.
    /// </summary>
    public DecisionEngineService(ISchemaValidationService schemaValidationService)
        : this(
            schemaValidationService ?? throw new ArgumentNullException(nameof(schemaValidationService)),
            new DecisionMergeInputGate(schemaValidationService),
            new AgentProposalManifestMerger(),
            new DecisionNodeManifestMerger(),
            new ManifestGovernanceMerger())
    {
    }

    public DecisionMergeResult MergeResults(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        string? parentManifestVersion = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(evaluations);
        ArgumentNullException.ThrowIfNull(decisionNodes);

        DecisionMergeResult output = new();

        if (!_mergeInputGate.TryValidateMergeInputs(runId, manifestVersion, results, output))
            return output;

        List<AgentResult> validResults = _mergeInputGate.ValidateAndFilterResults(runId, results, output);

        if (output.Errors.Count > 0)
            return output;

        if (!_mergeInputGate.ValidateAgentResultsAgainstSchema(validResults, output))
            return output;

        GoldenManifest manifest =
            GoldenManifestFactory.CreateBase(runId, request, manifestVersion, parentManifestVersion);

        _agentProposalManifestMerger.MergeAgentResultsIntoManifest(runId, validResults, manifest, output);

        _manifestGovernanceMerger.ApplyEvaluationSignals(runId, evaluations, validResults, output);

        _decisionNodeManifestMerger.ApplyDecisionNodes(runId, decisionNodes, manifest, output);

        _manifestGovernanceMerger.ApplyGovernanceDefaults(manifest, request, validResults, output);

        _manifestGovernanceMerger.EnsureRequiredControlsAreAppliedToRelevantComponents(manifest, output);

        DecisionTraceManifestAttachment.Attach(manifest, output.DecisionTraces);

        string manifestJson = SchemaValidationSerializer.Serialize(manifest);
        SchemaValidationResult manifestValidation = _schemaValidationService.ValidateGoldenManifestJson(manifestJson);

        if (!manifestValidation.IsValid)
        {
            output.Errors.AddRange(
                manifestValidation.Errors.Select(e => $"GoldenManifest validation failed: {e}"));
            return output;
        }

        output.Manifest = manifest;
        return output;
    }
}
