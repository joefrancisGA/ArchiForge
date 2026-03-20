using System.Text;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
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
        var sb = new StringBuilder();

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
        foreach (var item in manifest.Requirements.Covered)
        {
            sb.AppendLine($"- Covered: {item.RequirementName}");
        }
        foreach (var item in manifest.Requirements.Uncovered)
        {
            sb.AppendLine($"- Uncovered: {item.RequirementName}");
        }
        if (manifest.Requirements.Covered.Count == 0 && manifest.Requirements.Uncovered.Count == 0)
        {
            sb.AppendLine("- No requirements recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Topology");
        foreach (var pattern in manifest.Topology.SelectedPatterns)
        {
            sb.AppendLine($"- Pattern: {pattern}");
        }
        foreach (var resource in manifest.Topology.Resources)
        {
            sb.AppendLine($"- Resource: {resource}");
        }
        foreach (var gap in manifest.Topology.Gaps)
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
        foreach (var control in manifest.Security.Controls)
        {
            sb.AppendLine($"- {control.ControlName}: {control.Status}");
        }
        foreach (var gap in manifest.Security.Gaps)
        {
            sb.AppendLine($"- Gap: {gap}");
        }
        if (manifest.Security.Controls.Count == 0 && manifest.Security.Gaps.Count == 0)
        {
            sb.AppendLine("- No security posture recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Compliance");
        foreach (var control in manifest.Compliance.Controls)
        {
            sb.AppendLine($"- {control.ControlId} {control.ControlName}: {control.Status}");
        }
        foreach (var gap in manifest.Compliance.Gaps)
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
        foreach (var risk in manifest.Cost.CostRisks)
        {
            sb.AppendLine($"- Risk: {risk}");
        }
        sb.AppendLine();

        sb.AppendLine("## Decisions");
        foreach (var decision in manifest.Decisions)
        {
            sb.AppendLine($"- {decision.Category}: {decision.Title} -> {decision.SelectedOption}");
        }
        if (manifest.Decisions.Count == 0)
        {
            sb.AppendLine("- No decisions recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Unresolved Issues");
        foreach (var issue in manifest.UnresolvedIssues.Items)
        {
            sb.AppendLine($"- [{issue.Severity}] {issue.Title}: {issue.Description}");
        }
        if (manifest.UnresolvedIssues.Items.Count == 0)
        {
            sb.AppendLine("- No unresolved issues.");
        }

        var content = sb.ToString();

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
