using System.Text;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class ReferenceArchitectureMarkdownGenerator : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.ReferenceArchitectureMarkdown;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"# Reference Architecture - {manifest.Metadata.Name}");
        sb.AppendLine();
        sb.AppendLine($"Status: {manifest.Metadata.Status}");
        sb.AppendLine($"Version: {manifest.Metadata.Version}");
        sb.AppendLine();

        sb.AppendLine("## Manifest Metadata");
        sb.AppendLine($"- Rule Set: {manifest.RuleSetId} {manifest.RuleSetVersion}");
        sb.AppendLine($"- Manifest Hash: {manifest.ManifestHash}");
        sb.AppendLine();

        sb.AppendLine("## Requirements");
        foreach (RequirementCoverageItem item in manifest.Requirements.Covered)
        {
            sb.AppendLine($"- Covered: {item.RequirementName}");
        }
        foreach (RequirementCoverageItem item in manifest.Requirements.Uncovered)
        {
            sb.AppendLine($"- Uncovered: {item.RequirementName}");
        }
        if (manifest.Requirements.Covered.Count == 0 && manifest.Requirements.Uncovered.Count == 0)
        {
            sb.AppendLine("- No requirements recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Topology");
        foreach (string pattern in manifest.Topology.SelectedPatterns)
        {
            sb.AppendLine($"- Pattern: {pattern}");
        }
        foreach (string resource in manifest.Topology.Resources)
        {
            sb.AppendLine($"- Resource: {resource}");
        }
        foreach (string gap in manifest.Topology.Gaps)
        {
            sb.AppendLine($"- Gap: {gap}");
        }
        if (manifest.Topology.SelectedPatterns.Count == 0 &&
            manifest.Topology.Resources.Count == 0 &&
            manifest.Topology.Gaps.Count == 0)
        {
            sb.AppendLine("- No topology information recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Security");
        foreach (SecurityPostureItem control in manifest.Security.Controls)
        {
            sb.AppendLine($"- {control.ControlName}: {control.Status}");
        }
        foreach (string gap in manifest.Security.Gaps)
        {
            sb.AppendLine($"- Gap: {gap}");
        }
        if (manifest.Security.Controls.Count == 0 && manifest.Security.Gaps.Count == 0)
        {
            sb.AppendLine("- No security posture recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Compliance");
        foreach (CompliancePostureItem control in manifest.Compliance.Controls)
        {
            sb.AppendLine($"- {control.ControlId} {control.ControlName}: {control.Status}");
        }
        foreach (string gap in manifest.Compliance.Gaps)
        {
            sb.AppendLine($"- Gap: {gap}");
        }
        if (manifest.Compliance.Controls.Count == 0 && manifest.Compliance.Gaps.Count == 0)
        {
            sb.AppendLine("- No compliance posture recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Cost");
        sb.AppendLine($"- Max Monthly Cost: {(manifest.Cost.MaxMonthlyCost.HasValue ? manifest.Cost.MaxMonthlyCost.Value.ToString("0.00") : "Not specified")}");
        foreach (string risk in manifest.Cost.CostRisks)
        {
            sb.AppendLine($"- Risk: {risk}");
        }
        sb.AppendLine();

        sb.AppendLine("## Decisions");
        foreach (ResolvedArchitectureDecision decision in manifest.Decisions)
        {
            sb.AppendLine($"- {decision.Category}: {decision.Title} -> {decision.SelectedOption}");
        }
        if (manifest.Decisions.Count == 0)
        {
            sb.AppendLine("- No decisions recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Unresolved Issues");
        foreach (ManifestIssue issue in manifest.UnresolvedIssues.Items)
        {
            sb.AppendLine($"- [{issue.Severity}] {issue.Title}: {issue.Description}");
        }
        if (manifest.UnresolvedIssues.Items.Count == 0)
        {
            sb.AppendLine("- No unresolved issues.");
        }

        string content = sb.ToString();

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.ReferenceArchitectureMarkdown,
            Name = "reference-architecture.md",
            Format = "markdown",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content)
        });
    }
}
