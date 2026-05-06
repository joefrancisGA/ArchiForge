using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Applies evaluation signals, request-derived governance defaults, and propagates required controls to manifest
///     components.
/// </summary>
public sealed class ManifestGovernanceMerger
{
    private const string ControlPrivateEndpoints = "Private Endpoints";
    private const string ControlPrivateNetworking = "Private Networking";
    private const string ControlManagedIdentity = "Managed Identity";

    /// <summary>
    ///     Surfaces evaluation signals in the merge output: adds a trace entry per result
    ///     that received at least one evaluation, and promotes a warning when net opposition
    ///     is significant (net delta below -0.30).
    /// </summary>
    public void ApplyEvaluationSignals(
        string runId,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<AgentResult> results,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        foreach (AgentResult result in results)
        {
            List<AgentEvaluation> taskEvals = evaluations
                .Where(e => e.TargetAgentTaskId == result.TaskId)
                .ToList();

            if (taskEvals.Count == 0)
                continue;

            double netDelta = taskEvals.Sum(e => e.ConfidenceDelta);
            string types = string.Join(", ",
                taskEvals.Select(e => e.EvaluationType).Distinct(StringComparer.OrdinalIgnoreCase));

            DecisionMergeTraceRecorder.AddTrace(
                output,
                runId,
                "EvaluationSignalApplied",
                $"{result.AgentType} received {taskEvals.Count} evaluation(s) " +
                $"(net delta: {netDelta:+0.000;-0.000}): {types}",
                new Dictionary<string, string>
                {
                    ["resultId"] = result.ResultId,
                    ["agentType"] = result.AgentType.ToString(),
                    ["evaluationCount"] = taskEvals.Count.ToString(),
                    ["netConfidenceDelta"] = netDelta.ToString("F3")
                });

            if (netDelta < -0.30)

                output.Warnings.Add(
                    $"{result.AgentType} result '{result.ResultId}' received net opposition " +
                    $"signal ({netDelta:F3}); review decision traces for details.");
        }
    }

    public void ApplyGovernanceDefaults(
        GoldenManifest manifest,
        ArchitectureRequest request,
        IReadOnlyCollection<AgentResult> validResults,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(validResults);
        ArgumentNullException.ThrowIfNull(output);

        if (request.RequiredCapabilities.Any(c =>
                c.Contains("private", StringComparison.OrdinalIgnoreCase)))
            AddRequiredControlIfMissing(manifest, ControlPrivateNetworking, output);

        if (request.RequiredCapabilities.Any(c =>
                c.Contains("managed identity", StringComparison.OrdinalIgnoreCase)))
            AddRequiredControlIfMissing(manifest, ControlManagedIdentity, output);

        if (validResults.Any(r => r.AgentType == AgentType.Compliance))

            manifest.Governance.ComplianceTags =
                manifest.Governance.ComplianceTags
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
    }

    public void EnsureRequiredControlsAreAppliedToRelevantComponents(
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(output);

        foreach (string control in manifest.Governance.RequiredControls)
        {

            foreach (ManifestService service in manifest.Services.Where(service =>
                         !service.RequiredControls.Contains(control, StringComparer.OrdinalIgnoreCase)))
                service.RequiredControls.Add(control);

            if (!control.Equals(ControlPrivateEndpoints, StringComparison.OrdinalIgnoreCase) &&
                !control.Equals(ControlPrivateNetworking, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (ManifestDatastore datastore in manifest.Datastores)
                datastore.PrivateEndpointRequired = true;
        }

        DecisionMergeTraceRecorder.AddTrace(
            output,
            manifest.RunId,
            "GovernanceControlsApplied",
            "Applied governance required controls to relevant manifest components.",
            null);
    }

    private static void AddRequiredControlIfMissing(
        GoldenManifest manifest,
        string control,
        DecisionMergeResult output)
    {
        if (manifest.Governance.RequiredControls.Contains(control, StringComparer.OrdinalIgnoreCase))
            return;

        manifest.Governance.RequiredControls.Add(control);

        DecisionMergeTraceRecorder.AddTrace(
            output,
            manifest.RunId,
            "RequiredControlDefaulted",
            $"Added default required control '{control}'.",
            new Dictionary<string, string> { ["control"] = control });
    }
}

