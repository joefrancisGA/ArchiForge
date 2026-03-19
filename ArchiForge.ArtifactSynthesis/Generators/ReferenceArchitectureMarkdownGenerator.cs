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

        sb.AppendLine($"# {manifest.Metadata.Name}");
        sb.AppendLine();
        sb.AppendLine($"Status: {manifest.Metadata.Status}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine(manifest.Metadata.Summary);
        sb.AppendLine();

        sb.AppendLine("## Requirements Coverage");
        foreach (var item in manifest.Requirements.Covered)
        {
            sb.AppendLine($"- {item.RequirementName}: {item.CoverageStatus}");
        }
        if (manifest.Requirements.Covered.Count == 0)
        {
            sb.AppendLine("- No covered requirements.");
        }
        sb.AppendLine();

        sb.AppendLine("## Topology");
        foreach (var gap in manifest.Topology.Gaps)
        {
            sb.AppendLine($"- Gap: {gap}");
        }
        if (manifest.Topology.Gaps.Count == 0)
        {
            sb.AppendLine("- No topology gaps.");
        }
        sb.AppendLine();

        sb.AppendLine("## Security");
        foreach (var control in manifest.Security.Controls)
        {
            sb.AppendLine($"- {control.ControlName}: {control.Status}");
        }
        if (manifest.Security.Controls.Count == 0)
        {
            sb.AppendLine("- No security controls recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Cost");
        sb.AppendLine($"- Max Monthly Cost: {(manifest.Cost.MaxMonthlyCost.HasValue ? manifest.Cost.MaxMonthlyCost.Value.ToString("0.00") : "Not specified")}");
        foreach (var risk in manifest.Cost.CostRisks)
        {
            sb.AppendLine($"- Risk: {risk}");
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
        sb.AppendLine();

        sb.AppendLine("## Decisions");
        foreach (var decision in manifest.Decisions)
        {
            sb.AppendLine($"- {decision.Category}: {decision.Title} -> {decision.SelectedOption}");
        }
        if (manifest.Decisions.Count == 0)
        {
            sb.AppendLine("- No decisions.");
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
