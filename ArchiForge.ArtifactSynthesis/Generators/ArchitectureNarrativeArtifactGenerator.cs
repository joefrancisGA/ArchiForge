using System.Text;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class ArchitectureNarrativeArtifactGenerator : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.ArchitectureNarrative;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        var sb = new StringBuilder();

        sb.AppendLine($"# {manifest.Metadata.Name}");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine(manifest.Metadata.Summary);
        sb.AppendLine();

        sb.AppendLine("## Requirements Coverage");
        if (manifest.Requirements.Covered.Count == 0 && manifest.Requirements.Uncovered.Count == 0)
        {
            sb.AppendLine("No requirements were identified.");
        }
        else
        {
            foreach (var item in manifest.Requirements.Covered)
            {
                sb.AppendLine($"- Covered: {item.RequirementName}");
            }

            foreach (var item in manifest.Requirements.Uncovered)
            {
                sb.AppendLine($"- Uncovered: {item.RequirementName}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Topology Posture");
        if (manifest.Topology.Resources.Count > 0)
        {
            foreach (var resource in manifest.Topology.Resources)
            {
                sb.AppendLine($"- Resource: {resource}");
            }
        }
        else
        {
            sb.AppendLine("No concrete topology resources were recorded in the manifest.");
        }

        foreach (var gap in manifest.Topology.Gaps)
        {
            sb.AppendLine($"- Gap: {gap}");
        }
        sb.AppendLine();

        sb.AppendLine("## Security Posture");
        if (manifest.Security.Controls.Count == 0)
        {
            sb.AppendLine("No security controls were recorded.");
        }
        else
        {
            foreach (var control in manifest.Security.Controls)
            {
                sb.AppendLine($"- {control.ControlName}: {control.Status}");
            }
        }

        foreach (var gap in manifest.Security.Gaps)
        {
            sb.AppendLine($"- Security Gap: {gap}");
        }
        sb.AppendLine();

        sb.AppendLine("## Compliance Posture");
        if (manifest.Compliance.Controls.Count == 0)
        {
            sb.AppendLine("No compliance posture items were recorded.");
        }
        else
        {
            foreach (var control in manifest.Compliance.Controls)
            {
                sb.AppendLine($"- {control.ControlId} {control.ControlName}: {control.Status}");
            }
        }

        foreach (var gap in manifest.Compliance.Gaps)
        {
            sb.AppendLine($"- Compliance Gap: {gap}");
        }
        sb.AppendLine();

        sb.AppendLine("## Cost Posture");
        sb.AppendLine($"- Max Monthly Cost: {(manifest.Cost.MaxMonthlyCost.HasValue ? manifest.Cost.MaxMonthlyCost.Value.ToString("0.00") : "Not specified")}");
        foreach (var risk in manifest.Cost.CostRisks)
        {
            sb.AppendLine($"- Cost Risk: {risk}");
        }
        sb.AppendLine();

        sb.AppendLine("## Constraints");
        foreach (var item in manifest.Constraints.MandatoryConstraints)
        {
            sb.AppendLine($"- Mandatory: {item}");
        }
        foreach (var item in manifest.Constraints.Preferences)
        {
            sb.AppendLine($"- Preference: {item}");
        }
        if (manifest.Constraints.MandatoryConstraints.Count == 0 && manifest.Constraints.Preferences.Count == 0)
        {
            sb.AppendLine("No constraints were recorded.");
        }
        sb.AppendLine();

        sb.AppendLine("## Unresolved Issues");
        if (manifest.UnresolvedIssues.Items.Count == 0)
        {
            sb.AppendLine("No unresolved issues.");
        }
        else
        {
            foreach (var issue in manifest.UnresolvedIssues.Items)
            {
                sb.AppendLine($"- [{issue.Severity}] {issue.Title}: {issue.Description}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Provenance");
        sb.AppendLine($"- Source Findings: {manifest.Provenance.SourceFindingIds.Count}");
        sb.AppendLine($"- Source Graph Nodes: {manifest.Provenance.SourceGraphNodeIds.Count}");
        sb.AppendLine($"- Applied Rules: {manifest.Provenance.AppliedRuleIds.Count}");
        sb.AppendLine();

        var content = sb.ToString();

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.ArchitectureNarrative,
            Name = "architecture-narrative.md",
            Format = "markdown",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content)
        });
    }
}
