using System.Text;
using System.Text.Json;

using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Alerts;

namespace ArchLucid.Decisioning.Advisory.Scheduling;

/// <inheritdoc cref="IArchitectureDigestBuilder" />
public sealed class ArchitectureDigestBuilder : IArchitectureDigestBuilder
{
    private const string HeadingDigest = "# Daily Architecture Digest";
    private const string HeadingSummary = "## Summary";
    private const string HeadingTopRecommendations = "## Top Recommendations";
    private const string HeadingAlerts = "## Alerts";
    private const string DigestTitle = "Daily Architecture Digest";
    private const string NoRecommendationsNote = "No significant recommendations were identified.";
    private const string NoAlertsNote = "No alerts were triggered.";
    private const string NoIssuesSummary = "No major architecture issues were identified in the latest scan.";
    private const int TopRecommendationCount = 5;

    /// <inheritdoc />
    /// <remarks>
    ///     Includes up to five highest-priority recommendations, all summary notes, and every alert as a bullet line.
    ///     <see cref="ArchitectureDigest.MetadataJson" /> stores counts: recommendation totals, top slice size, evaluated
    ///     alerts, and high/critical alert count.
    /// </remarks>
    public ArchitectureDigest Build(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId,
        ImprovementPlan plan,
        IReadOnlyList<AlertRecord>? evaluatedAlerts = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        List<ImprovementRecommendation> top = plan.Recommendations
            .OrderByDescending(x => x.PriorityScore)
            .Take(TopRecommendationCount)
            .ToList();

        StringBuilder sb = new();
        sb.AppendLine(HeadingDigest);
        sb.AppendLine();
        sb.AppendLine($"Generated: {plan.GeneratedUtc:u}");
        if (comparedToRunId.HasValue)
            sb.AppendLine($"Compared to prior run: {comparedToRunId:N}");
        sb.AppendLine();

        sb.AppendLine(HeadingSummary);
        foreach (string note in plan.SummaryNotes)
            sb.AppendLine($"- {note}");
        sb.AppendLine();

        sb.AppendLine(HeadingTopRecommendations);
        if (top.Count == 0)

            sb.AppendLine(NoRecommendationsNote);

        else

            foreach (ImprovementRecommendation item in top)
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

        IReadOnlyList<AlertRecord> alerts = evaluatedAlerts ?? [];
        int highCritical = alerts
            .Count(a =>
                string.Equals(a.Severity, AlertSeverity.High, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Severity, AlertSeverity.Critical, StringComparison.OrdinalIgnoreCase));

        sb.AppendLine(HeadingAlerts);
        if (alerts.Count == 0)

            sb.AppendLine(NoAlertsNote);

        else

            foreach (AlertRecord alert in alerts)
                sb.AppendLine($"- [{alert.Severity}] {alert.Title} — {alert.Description}");

        sb.AppendLine();

        string summary = top.Count == 0
            ? NoIssuesSummary
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
            Title = DigestTitle,
            Summary = summary,
            ContentMarkdown = sb.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                recommendationCount = plan.Recommendations.Count,
                topRecommendationCount = top.Count,
                evaluatedAlertCount = alerts.Count,
                highOrCriticalAlertCount = highCritical
            })
        };
    }
}
