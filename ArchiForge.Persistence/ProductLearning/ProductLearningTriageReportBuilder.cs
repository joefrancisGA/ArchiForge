using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Persistence.ProductLearning;

/// <summary>
/// Builds a concise triage report document from a full <see cref="LearningDashboardSummary"/> (deterministic ordering).
/// </summary>
public static class ProductLearningTriageReportBuilder
{
    public static ProductLearningTriageReportDocument Build(
        LearningDashboardSummary summary,
        ProductLearningTriageReportLimits limits,
        DateTime? sinceUtc)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(limits);

        IReadOnlyList<ProductLearningTriageReportArtifactRow> artifacts = SelectArtifactRows(summary, limits);
        IReadOnlyList<string> problemAreas = BuildProblemAreas(summary, limits);
        IReadOnlyList<ProductLearningTriageReportImprovementLine> improvements = SelectImprovements(summary, limits);
        IReadOnlyList<ProductLearningTriageReportTriageLine> triage = SelectTriage(summary, limits);

        return new ProductLearningTriageReportDocument
        {
            GeneratedUtc = summary.GeneratedUtc,
            TenantId = summary.TenantId,
            WorkspaceId = summary.WorkspaceId,
            ProjectId = summary.ProjectId,
            SinceUtc = sinceUtc,
            TotalSignalsInScope = summary.TotalSignalsInScope,
            DistinctRunsReviewed = summary.DistinctRunsTouched,
            ArtifactOutcomes = artifacts,
            TopProblemAreas = problemAreas,
            TopImprovements = improvements,
            TriageQueuePreview = triage,
        };
    }

    private static IReadOnlyList<ProductLearningTriageReportArtifactRow> SelectArtifactRows(
        LearningDashboardSummary summary,
        ProductLearningTriageReportLimits limits)
    {
        int maxRows = Math.Max(1, limits.MaxArtifactRows);

        List<ArtifactOutcomeTrend> ordered = summary.ArtifactTrends
            .OrderByDescending(static t => t.RevisionCount + t.RejectionCount + t.NeedsFollowUpCount)
            .ThenBy(static t => t.TrendKey, StringComparer.Ordinal)
            .Take(maxRows)
            .ToList();

        List<ProductLearningTriageReportArtifactRow> rows = new(ordered.Count);

        foreach (ArtifactOutcomeTrend t in ordered)
        {
            rows.Add(
                new ProductLearningTriageReportArtifactRow
                {
                    ArtifactLabel = string.IsNullOrWhiteSpace(t.ArtifactTypeOrHint) ? t.TrendKey : t.ArtifactTypeOrHint,
                    Trusted = t.AcceptedOrTrustedCount,
                    Revised = t.RevisionCount,
                    Rejected = t.RejectionCount,
                    FollowUp = t.NeedsFollowUpCount,
                    Runs = t.DistinctRunCount,
                    ThemeHint = TrimHint(t.RepeatedThemeIndicator, 120),
                });
        }

        return rows;
    }

    private static IReadOnlyList<string> BuildProblemAreas(
        LearningDashboardSummary summary,
        ProductLearningTriageReportLimits limits)
    {
        int maxLines = Math.Max(1, limits.MaxProblemAreaLines);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> lines = new();

        IEnumerable<FeedbackAggregate> aggOrdered = summary.TopAggregates
            .OrderByDescending(static a => a.RejectedCount + a.RevisedCount + a.NeedsFollowUpCount)
            .ThenByDescending(static a => a.TotalSignalCount)
            .ThenBy(static a => a.AggregateKey, StringComparer.Ordinal);

        foreach (FeedbackAggregate a in aggOrdered)
        {
            if (lines.Count >= maxLines)
            {
                break;
            }

            string line = FormatAggregateProblemLine(a);

            if (line.Length == 0 || !seen.Add(line))
            {
                continue;
            }

            lines.Add(Truncate(line, 200));
        }

        foreach (ImprovementOpportunity o in summary.Opportunities.OrderBy(static o => o.PriorityRank).ThenBy(static o => o.Title, StringComparer.Ordinal))
        {
            if (lines.Count >= maxLines)
            {
                break;
            }

            string line = Truncate((o.Title ?? string.Empty).Trim(), 200);

            if (line.Length == 0 || !seen.Add(line))
            {
                continue;
            }

            lines.Add(line);
        }

        return lines;
    }

    private static string FormatAggregateProblemLine(FeedbackAggregate a)
    {
        string area = (a.SubjectTypeOrWorkflowArea ?? string.Empty).Trim();

        if (area.Length == 0)
        {
            area = "Feedback";
        }

        string? pattern = string.IsNullOrWhiteSpace(a.PatternKey) ? null : a.PatternKey.Trim();
        string baseLine = pattern is null ? area : area + " — pattern `" + pattern + "`";

        if (!string.IsNullOrWhiteSpace(a.DominantThemeHint))
        {
            string? hint = TrimHint(a.DominantThemeHint, 80);

            if (hint is not null)
            {
                baseLine += " (" + hint + ")";
            }
        }

        return baseLine;
    }

    private static IReadOnlyList<ProductLearningTriageReportImprovementLine> SelectImprovements(
        LearningDashboardSummary summary,
        ProductLearningTriageReportLimits limits)
    {
        int max = Math.Max(1, limits.MaxImprovements);
        int cap = limits.MaxSummaryChars < 40 ? 40 : limits.MaxSummaryChars;

        return summary.Opportunities
            .OrderBy(static o => o.PriorityRank)
            .ThenBy(static o => o.Title, StringComparer.Ordinal)
            .Take(max)
            .Select(
                o => new ProductLearningTriageReportImprovementLine
                {
                    Title = (o.Title ?? string.Empty).Trim(),
                    Severity = (o.Severity ?? string.Empty).Trim(),
                    Area = (o.AffectedArtifactTypeOrWorkflowArea ?? string.Empty).Trim(),
                    Summary = Truncate((o.Summary ?? string.Empty).Trim(), cap),
                })
            .ToList();
    }

    private static IReadOnlyList<ProductLearningTriageReportTriageLine> SelectTriage(
        LearningDashboardSummary summary,
        ProductLearningTriageReportLimits limits)
    {
        int max = Math.Max(1, limits.MaxTriagePreview);

        return summary.TriageQueue
            .OrderBy(static i => i.PriorityRank)
            .ThenBy(static i => i.Title, StringComparer.Ordinal)
            .Take(max)
            .Select(
                i => new ProductLearningTriageReportTriageLine
                {
                    Rank = i.PriorityRank,
                    Title = (i.Title ?? string.Empty).Trim(),
                    Severity = (i.Severity ?? string.Empty).Trim(),
                    DetailSummary = Truncate((i.DetailSummary ?? string.Empty).Trim(), 280),
                    SuggestedNextStep = string.IsNullOrWhiteSpace(i.SuggestedNextAction)
                        ? null
                        : Truncate(i.SuggestedNextAction.Trim(), 200),
                })
            .ToList();
    }

    private static string? TrimHint(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Truncate(value.Trim(), maxLen);
    }

    private static string Truncate(string value, int maxLen)
    {
        if (value.Length <= maxLen)
        {
            return value;
        }

        return value[..(maxLen - 1)] + "…";
    }
}
