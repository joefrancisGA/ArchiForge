using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Applies coordinator <see cref="DecisionNode" /> outcomes to a <see cref="GoldenManifest" /> and records traces.
/// </summary>
public sealed class DecisionNodeManifestMerger
{
    private const string TopicTopologyAcceptance = "TopologyAcceptance";
    private const string TopicSecurityControlPromotion = "SecurityControlPromotion";
    private const string TopicComplexityDisposition = "ComplexityDisposition";
    private const string EventTypeDecisionResolution = "DecisionResolution";
    private const string ControlPrivateEndpoints = "Private Endpoints";
    private const string ControlManagedIdentity = "Managed Identity";

    public void ApplyDecisionNodes(
        string runId,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(decisionNodes);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(output);

        foreach (IGrouping<string, DecisionNode> dup in decisionNodes
                     .GroupBy(d => d.Topic, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))

            output.Warnings.Add(
                $"Decision topic '{dup.Key}' has {dup.Count()} duplicate nodes; only the first will be applied.");


        DecisionNode? topologyAcceptance = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, TopicTopologyAcceptance, StringComparison.OrdinalIgnoreCase));

        if (topologyAcceptance is not null)
        {
            DecisionOption? selected =
                topologyAcceptance.Options.FirstOrDefault(o => o.OptionId == topologyAcceptance.SelectedOptionId);

            if (selected is null && !string.IsNullOrWhiteSpace(topologyAcceptance.SelectedOptionId))
            {
                output.Errors.Add(
                    $"{TopicTopologyAcceptance} node has SelectedOptionId '{topologyAcceptance.SelectedOptionId}' " +
                    "that does not match any option. Merge aborted to prevent corrupt decision semantics.");
                return;
            }

            if (selected is not null &&
                selected.Description.StartsWith("Reject", StringComparison.OrdinalIgnoreCase))
            {
                output.Errors.Add("Topology proposal was rejected by Decision Engine v2.");
                return;
            }

            DecisionMergeTraceRecorder.AddTrace(
                output,
                runId,
                EventTypeDecisionResolution,
                $"{TopicTopologyAcceptance}: {selected?.Description ?? "Unknown"} | {topologyAcceptance.Rationale}",
                new Dictionary<string, string>
                {
                    ["decisionTopic"] = TopicTopologyAcceptance,
                    ["outcome"] = selected?.Description ?? "Unknown",
                    ["confidence"] = topologyAcceptance.Confidence.ToString("F3")
                });
        }

        DecisionNode? securityPromotion = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, TopicSecurityControlPromotion, StringComparison.OrdinalIgnoreCase));

        if (securityPromotion is not null)
        {
            DecisionOption? selected =
                securityPromotion.Options.FirstOrDefault(o => o.OptionId == securityPromotion.SelectedOptionId);

            if (selected is not null)
            {
                if (selected.Description.Contains(ControlPrivateEndpoints, StringComparison.OrdinalIgnoreCase) &&
                    !manifest.Governance.RequiredControls.Contains(ControlPrivateEndpoints,
                        StringComparer.OrdinalIgnoreCase))
                    manifest.Governance.RequiredControls.Add(ControlPrivateEndpoints);

                if (selected.Description.Contains(ControlManagedIdentity, StringComparison.OrdinalIgnoreCase) &&
                    !manifest.Governance.RequiredControls.Contains(ControlManagedIdentity,
                        StringComparer.OrdinalIgnoreCase))
                    manifest.Governance.RequiredControls.Add(ControlManagedIdentity);

                DecisionMergeTraceRecorder.AddTrace(
                    output,
                    runId,
                    EventTypeDecisionResolution,
                    $"{TopicSecurityControlPromotion}: {selected.Description} | {securityPromotion.Rationale}",
                    new Dictionary<string, string>
                    {
                        ["decisionTopic"] = TopicSecurityControlPromotion,
                        ["outcome"] = selected.Description,
                        ["confidence"] = securityPromotion.Confidence.ToString("F3")
                    });
            }
        }

        DecisionNode? complexityDecision = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, TopicComplexityDisposition, StringComparison.OrdinalIgnoreCase));

        if (complexityDecision is null)
            return;

        DecisionOption? complexitySelected =
            complexityDecision.Options.FirstOrDefault(o => o.OptionId == complexityDecision.SelectedOptionId);

        if (complexitySelected is not null &&
            complexitySelected.Description.Contains("Reduce complexity", StringComparison.OrdinalIgnoreCase))
            manifest.Governance.PolicyConstraints.Add("Review architecture scope for MVP complexity reduction.");

        DecisionMergeTraceRecorder.AddTrace(
            output,
            runId,
            EventTypeDecisionResolution,
            $"{TopicComplexityDisposition}: {complexitySelected?.Description ?? "Unknown"} | {complexityDecision.Rationale}",
            new Dictionary<string, string>
            {
                ["decisionTopic"] = TopicComplexityDisposition,
                ["outcome"] = complexitySelected?.Description ?? "Unknown",
                ["confidence"] = complexityDecision.Confidence.ToString("F3")
            });
    }
}
