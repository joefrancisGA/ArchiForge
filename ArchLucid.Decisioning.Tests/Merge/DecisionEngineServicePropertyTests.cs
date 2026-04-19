using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Decisioning.Validation;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Decisioning.Tests.Merge;

/// <summary>
/// FsCheck properties for <see cref="DecisionEngineService.MergeResults"/> on the minimal valid path.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DecisionEngineServicePropertyTests
{
    [Property(MaxTest = 100)]
    public Property Minimal_merge_is_idempotent()
    {
        Gen<string> runIdGen = Arb.Default.String().Generator.Where(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 64);
        Gen<string> manifestVerGen = Arb.Default.String().Generator.Where(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 32);

        return Prop.ForAll(
            Arb.From(runIdGen),
            Arb.From(manifestVerGen),
            (runId, manifestVersion) =>
            {
                DecisionEngineService sut = new(new PassthroughSchemaValidationService());
                ArchitectureRequest request = MinimalRequest();
                AgentResult[] results = [ValidResult(runId)];

                DecisionMergeResult first = sut.MergeResults(runId, request, manifestVersion, results, [], []);
                DecisionMergeResult second = sut.MergeResults(runId, request, manifestVersion, results, [], []);

                if (!first.Success || !second.Success)
                {
                    return false;
                }

                return first.Manifest.RunId == second.Manifest.RunId
                       && first.Manifest.Metadata.ManifestVersion == second.Manifest.Metadata.ManifestVersion;
            });
    }

    private static ArchitectureRequest MinimalRequest() =>
        new()
        {
            RequestId = "req-prop",
            SystemName = "Sys",
            Description = "A long enough description for validation.",
            Environment = "prod"
        };

    private static AgentResult ValidResult(string runId) =>
        new()
        {
            ResultId = "res-prop",
            TaskId = "task-prop",
            RunId = runId,
            AgentType = AgentType.Topology,
            Confidence = 0.9,
            Claims = ["ok"],
            EvidenceRefs = ["e1"]
        };
}
