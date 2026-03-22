using System.Text;
using System.Text.Json;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Advisory.Scheduling;

public sealed class ArchitectureDigestBuilder : IArchitectureDigestBuilder
{
    public ArchitectureDigest Build(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId,
        ImprovementPlan plan,
        IReadOnlyList<AlertRecord>? evaluatedAlerts = null)
    {
        var top = plan.Recommendations
            .OrderByDescending(x => x.PriorityScore)
            .Take(5)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# Daily Architecture Digest");
        sb.AppendLine();
        sb.AppendLine($"Generated: {plan.GeneratedUtc:u}");
        if (comparedToRunId.HasValue)
            sb.AppendLine($"Compared to prior run: {comparedToRunId:N}");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        foreach (var note in plan.SummaryNotes)
            sb.AppendLine($"- {note}");
        sb.AppendLine();

        sb.AppendLine("## Top Recommendations");
        if (top.Count == 0)
        {
            sb.AppendLine("No significant recommendations were identified.");
        }
        else
        {
            foreach (var item in top)
            {
                sb.AppendLine($"### {item.Title}");
                sb.AppendLine($"- Category: {item.Category}");
                sb.AppendLine($"- Urgency: {item.Urgency}");
                sb.AppendLine($"- Priority: {item.PriorityScore}");
                sb.AppendLine($"- Rationale: {item.Rationale}");
                sb.AppendLine($"- Suggested Action: {item.SuggestedAction}");
                sb.AppendLine($"- Expected Impact: {item.ExpectedImpact}");
                sb.AppendLine();
            }
        }

        var alerts = evaluatedAlerts ?? Array.Empty<AlertRecord>();
        var highCritical = alerts
            .Count(a =>
                string.Equals(a.Severity, AlertSeverity.High, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Severity, AlertSeverity.Critical, StringComparison.OrdinalIgnoreCase));

        sb.AppendLine("## Alerts");
        if (alerts.Count == 0)
        {
            sb.AppendLine("No alerts were triggered.");
        }
        else
        {
            foreach (var alert in alerts)
                sb.AppendLine($"- [{alert.Severity}] {alert.Title} — {alert.Description}");
        }

        sb.AppendLine();

        var summary = top.Count == 0
            ? "No major architecture issues were identified in the latest scan."
            : $"Top advisory items: {string.Join("; ", top.Select(x => x.Title))}";

        return new ArchitectureDigest
        {
            DigestId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = runId,
            ComparedToRunId = comparedToRunId,
            GeneratedUtc = plan.GeneratedUtc,
            Title = "Daily Architecture Digest",
            Summary = summary,
            ContentMarkdown = sb.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                recommendationCount = plan.Recommendations.Count,
                topRecommendationCount = top.Count,
                evaluatedAlertCount = alerts.Count,
                highOrCriticalAlertCount = highCritical,
            })
        };
    }
}
