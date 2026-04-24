using System.Text.Json.Serialization;

using ArchLucid.Decisioning.Manifest.Sections;

namespace ArchLucid.ArtifactSynthesis.Tests.GoldenCorpus;

internal sealed class SynthesisGoldenInputDocument
{
    [JsonPropertyName("runId")]
    public Guid RunId
    {
        get;
        set;
    }

    [JsonPropertyName("manifestId")]
    public Guid ManifestId
    {
        get;
        set;
    }

    [JsonPropertyName("coveredRequirementNames")]
    public List<string> CoveredRequirementNames
    {
        get;
        set;
    } = [];

    [JsonPropertyName("uncoveredRequirementNames")]
    public List<string> UncoveredRequirementNames
    {
        get;
        set;
    } = [];

    [JsonPropertyName("securityGaps")]
    public List<string> SecurityGaps
    {
        get;
        set;
    } = [];

    [JsonPropertyName("complianceGaps")]
    public List<string> ComplianceGaps
    {
        get;
        set;
    } = [];

    [JsonPropertyName("unresolvedIssues")]
    public List<SynthesisGoldenIssue> UnresolvedIssues
    {
        get;
        set;
    } = [];

    [JsonPropertyName("topologyGaps")]
    public List<string> TopologyGaps
    {
        get;
        set;
    } = [];
}

internal sealed class SynthesisGoldenIssue
{
    [JsonPropertyName("title")]
    public string Title
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("issueType")]
    public string IssueType
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("description")]
    public string Description
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("severity")]
    public string Severity
    {
        get;
        set;
    } = null!;
}

internal static class SynthesisGoldenManifestBuilder
{
    public static ArchLucid.Decisioning.Models.GoldenManifest Build(SynthesisGoldenInputDocument input)
    {
        List<RequirementCoverageItem> covered = input.CoveredRequirementNames
            .Select(
                n => new RequirementCoverageItem
                {
                    RequirementName = n,
                    RequirementText = n,
                    IsMandatory = true,
                    CoverageStatus = "Covered"
                })
            .ToList();

        List<RequirementCoverageItem> uncovered = input.UncoveredRequirementNames
            .Select(
                n => new RequirementCoverageItem
                {
                    RequirementName = n,
                    RequirementText = n,
                    IsMandatory = true,
                    CoverageStatus = "Uncovered"
                })
            .ToList();

        List<ManifestIssue> issues = input.UnresolvedIssues
            .Select(
                i => new ManifestIssue
                {
                    Title = i.Title,
                    IssueType = i.IssueType,
                    Description = i.Description,
                    Severity = i.Severity
                })
            .ToList();

        return new ArchLucid.Decisioning.Models.GoldenManifest
        {
            RunId = input.RunId,
            ManifestId = input.ManifestId,
            Requirements = new() { Covered = covered, Uncovered = uncovered },
            Security = new() { Gaps = [.. input.SecurityGaps] },
            Compliance = new() { Gaps = [.. input.ComplianceGaps] },
            UnresolvedIssues = new() { Items = issues },
            Topology = new() { Gaps = [.. input.TopologyGaps] }
        };
    }
}
