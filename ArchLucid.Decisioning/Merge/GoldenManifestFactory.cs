using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Builds the empty golden manifest shell before agent proposals and decision nodes are applied.
/// </summary>
public static class GoldenManifestFactory
{
    public static GoldenManifest CreateBase(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        string? parentManifestVersion)
    {
        return new GoldenManifest
        {
            RunId = runId,
            SystemName = request.SystemName,
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance =
                new ManifestGovernance
                {
                    ComplianceTags = [],
                    PolicyConstraints = request.Constraints.ToList(),
                    RequiredControls = [],
                    RiskClassification = "Moderate",
                    CostClassification = "Moderate"
                },
            Metadata = new ManifestMetadata
            {
                ManifestVersion = manifestVersion,
                ParentManifestVersion = parentManifestVersion,
                ChangeDescription = $"Merged manifest for run {runId}",
                DecisionTraceIds = [],
                CreatedUtc = DateTime.UtcNow
            }
        };
    }
}
