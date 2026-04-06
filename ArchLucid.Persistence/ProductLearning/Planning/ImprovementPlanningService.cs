using System.Globalization;
using System.Text;

using ArchiForge.Contracts.ProductLearning.Planning;

namespace ArchiForge.Persistence.ProductLearning.Planning;

/// <inheritdoc />
public sealed class ImprovementPlanningService : IImprovementPlanningService
{
    public Task<IReadOnlyList<ImprovementPlan>> BuildPlansAsync(
        IReadOnlyList<ImprovementThemeWithEvidence> themes,
        ImprovementPlanningOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(themes);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.RuleVersion))
        
            throw new ArgumentException("RuleVersion is required.", nameof(options));
        

        int maxSteps = options.MaxStepsPerPlan < 1 ? 1 : Math.Min(options.MaxStepsPerPlan, 20);
        DateTime createdUtc = options.CreatedUtcOverride ?? DateTime.UtcNow;
        string ruleVersion = options.RuleVersion.Trim();

        List<ImprovementThemeWithEvidence> ordered = themes
            .OrderBy(static t => t.Theme.ThemeId)
            .ThenBy(static t => t.CanonicalKey, StringComparer.Ordinal)
            .ToList();

        List<ImprovementPlan> plans = new(ordered.Count);

        foreach (ImprovementThemeWithEvidence item in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            plans.Add(BuildPlan(item, maxSteps, createdUtc, ruleVersion));
        }

        return Task.FromResult<IReadOnlyList<ImprovementPlan>>(plans);
    }

    private static ImprovementPlan BuildPlan(
        ImprovementThemeWithEvidence item,
        int maxSteps,
        DateTime createdUtc,
        string ruleVersion)
    {
        ImprovementTheme theme = item.Theme;
        string canonicalKey = item.CanonicalKey;

        string prefix = ClassifyPrefix(canonicalKey);
        (int frequencyScore, int severityScore, double trustImpact) = ComputeScores(theme, prefix);

        Guid planId = ImprovementPlanningDeterministicIds.PlanId(theme.ThemeId, canonicalKey, ruleVersion);

        string title = BuildTitle(theme, canonicalKey);
        string description = BuildDescription(item, prefix);

        IReadOnlyList<ImprovementPlanStep> steps = BuildSteps(item, prefix, maxSteps);

        return new ImprovementPlan
        {
            PlanId = planId,
            ThemeId = theme.ThemeId,
            Title = title,
            Description = description,
            ProposedChanges = steps,
            PriorityScore = frequencyScore + severityScore,
            FrequencyScore = frequencyScore,
            SeverityScore = severityScore,
            TrustImpactScore = trustImpact,
            CreatedUtc = createdUtc,
        };
    }

    private static string ClassifyPrefix(string canonicalKey)
    {
        if (canonicalKey.StartsWith("rollup:", StringComparison.Ordinal))
        
            return "rollup";
        

        if (canonicalKey.StartsWith("trend:", StringComparison.Ordinal))
        
            return "trend";
        

        if (canonicalKey.StartsWith("comment:", StringComparison.Ordinal))
        
            return "comment";
        

        if (canonicalKey.StartsWith("tag:", StringComparison.Ordinal))
        
            return "tag";
        

        return "unknown";
    }

    private static (int FrequencyScore, int SeverityScore, double TrustImpactScore) ComputeScores(
        ImprovementTheme theme,
        string prefix)
    {
        int frequency = Math.Min(theme.EvidenceCount * 30, 600);

        int severity = prefix switch
        {
            "rollup" => 320,
            "trend" => 280,
            "comment" => 240,
            "tag" => 200,
            _ => 160,
        };

        return (frequency, severity, 0d);
    }

    private static string BuildTitle(ImprovementTheme theme, string canonicalKey)
    {
        if (!string.IsNullOrWhiteSpace(theme.Name))
        
            return Truncate(theme.Name.Trim(), 200);
        

        return "Improvement plan for " + Truncate(canonicalKey, 120);
    }

    private static string BuildDescription(ImprovementThemeWithEvidence item, string prefix)
    {
        ImprovementTheme theme = item.Theme;
        IReadOnlyList<ImprovementThemeEvidence> sample = item.ExampleEvidence;

        int distinctRuns = sample
            .Select(static e => e.ArchitectureRunId)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .Count();

        int distinctSignals = sample
            .Select(static e => e.SignalId)
            .Where(static id => id is not null && id.Value != Guid.Empty)
            .Distinct()
            .Count();

        string window =
            theme.FirstSeenUtc.ToString("O", CultureInfo.InvariantCulture) + " to "
            + theme.LastSeenUtc.ToString("O", CultureInfo.InvariantCulture);

        string areas = theme.AffectedArtifactTypes.Count == 0
            ? "None listed; keep scope explicit with pilots before changing templates."
            : string.Join(
                "; ",
                theme.AffectedArtifactTypes.OrderBy(static s => s, StringComparer.Ordinal));

        StringBuilder builder = new();

        builder.Append("Problem statement: ");
        builder.AppendLine(BuildProblemStatement(theme, item.CanonicalKey, prefix));
        builder.Append("Evidence summary: Theme aggregates ");
        builder.Append(theme.EvidenceCount);
        builder.Append(" pilot signal(s). Example slice includes ");
        builder.Append(distinctRuns);
        builder.Append(" distinct architecture run id(s) and ");
        builder.Append(distinctSignals);
        builder.Append(" signal id(s). Observation window (UTC): ");
        builder.AppendLine(window);
        builder.Append("Affected system areas: ");
        builder.AppendLine(areas);
        builder.Append("Theme detail (from extraction): ");
        builder.AppendLine(Truncate((theme.Description ?? string.Empty).Trim(), 800));
        builder.Append("Grouping rule: ");
        builder.Append(Truncate((item.GroupingExplanation ?? string.Empty).Trim(), 500));

        return builder.ToString();
    }

    private static string BuildProblemStatement(ImprovementTheme theme, string canonicalKey, string prefix)
    {
        return prefix switch
        {
            "rollup" =>
                "Pilots repeated negative or revision outcomes on the same workflow or pattern bucket ("
                + Truncate(canonicalKey, 160) + ").",

            "trend" =>
                "Pilots reported repeated friction on a specific artifact facet (trend key in canonical id). "
                + "Review outputs for clarity, structure, and naming before changing automation.",

            "comment" =>
                "The same leading text appeared across multiple pilot comments; treat as a concrete wording or comprehension issue.",

            "tag" =>
                "Multiple pilots labeled signals with the same tag or annotation token in DetailJson; track as an explicit initiative.",

            _ =>
                "Feedback clustering produced this theme; validate the canonical key and pilot context before acting.",
        };
    }

    private static IReadOnlyList<ImprovementPlanStep> BuildSteps(
        ImprovementThemeWithEvidence item,
        string prefix,
        int maxSteps)
    {
        string facet = PrimaryFacetLabel(item.Theme);

        ImprovementPlanStep[] template = prefix switch
        {
            "rollup" => RollupSteps(facet),
            "trend" => TrendSteps(facet),
            "comment" => CommentSteps(),
            "tag" => TagSteps(facet),
            _ => GenericSteps(facet),
        };

        return template
            .Take(maxSteps)
            .ToList();
    }

    private static string PrimaryFacetLabel(ImprovementTheme theme)
    {
        string? first = theme.AffectedArtifactTypes
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .OrderBy(static s => s, StringComparer.Ordinal)
            .FirstOrDefault();

        if (first is null)
        
            return "pilot-facing outputs";
        

        return first.Trim();
    }

    private static ImprovementPlanStep[] RollupSteps(string facet) =>
    [
        new()
        {
            Ordinal = 1,
            ActionType = "Investigate",
            Title = "Confirm pilot scope and evidence",
            Description =
                "Open the example signals and runs listed for this theme. Record which manifest versions, exports, or diagrams pilots rejected, revised, or flagged for follow-up.",
        },
        new()
        {
            Ordinal = 2,
            ActionType = "Analyze",
            Title = "Map pattern to concrete deliverables",
            Description =
                "Translate pattern key or workflow area into specific templates, sections, or operator steps that must change for "
                + facet + ".",
        },
        new()
        {
            Ordinal = 3,
            ActionType = "UX",
            Title = "Improve readability and structure",
            Description =
                "Adjust headings, section order, and density so pilots can scan results in under two minutes. Add whitespace or summaries where pilots stalled.",
        },
        new()
        {
            Ordinal = 4,
            ActionType = "Policy",
            Title = "Publish acceptance criteria",
            Description =
                "Write a short checklist pilots use when approving outputs (required sections, naming, maximum length). Store it where reviewers can link from the run.",
        },
        new()
        {
            Ordinal = 5,
            ActionType = "Verify",
            Title = "Re-run a pilot slice",
            Description =
                "Collect at least three pilot reviews on the updated outputs. Compare disposition counts versus the baseline window before promoting wider.",
        },
    ];

    private static ImprovementPlanStep[] TrendSteps(string facet) =>
    [
        new()
        {
            Ordinal = 1,
            ActionType = "Investigate",
            Title = "Review artifact facet metrics",
            Description =
                "Use the trend counts for " + facet + " to confirm whether rejections, revisions, or follow-ups cluster on the same artifact type or export format.",
        },
        new()
        {
            Ordinal = 2,
            ActionType = "Content",
            Title = "Add structured sections",
            Description =
                "Insert explicit sections (context, decisions, risks, next steps) where pilots repeatedly asked for more structure in this facet.",
        },
        new()
        {
            Ordinal = 3,
            ActionType = "UX",
            Title = "Improve artifact readability",
            Description =
                "Shorten paragraphs, add bullet lists for requirements, and ensure diagrams or tables include captions tied to manifest metadata.",
        },
        new()
        {
            Ordinal = 4,
            ActionType = "Content",
            Title = "Improve naming clarity",
            Description =
                "Rename files, headings, and labels that pilots confused with similar artifacts. Document the naming map in release notes for operators.",
        },
        new()
        {
            Ordinal = 5,
            ActionType = "Verify",
            Title = "Pilot sign-off on layout",
            Description =
                "Run a structured review session. Record go/no-go per pilot and attach dispositions back into product-learning signals after the change.",
        },
    ];

    private static ImprovementPlanStep[] CommentSteps() =>
    [
        new()
        {
            Ordinal = 1,
            ActionType = "Investigate",
            Title = "Validate the repeated wording",
            Description =
                "Compare the shared comment prefix with actual outputs. Identify whether the issue is missing data, confusing labels, or policy gaps.",
        },
        new()
        {
            Ordinal = 2,
            ActionType = "UX",
            Title = "Update visible copy",
            Description =
                "Change UI strings, export headers, or inline help so the question behind the repeated comment is answered before pilots file feedback.",
        },
        new()
        {
            Ordinal = 3,
            ActionType = "Content",
            Title = "Add structured answers",
            Description =
                "If pilots ask the same question, embed a short, authoritative section in the artifact that references the source manifest fields.",
        },
        new()
        {
            Ordinal = 4,
            ActionType = "Verify",
            Title = "Confirm with pilots",
            Description =
                "Ask pilots to re-run the scenario. Close the theme only when the repeated prefix stops appearing in new signals.",
        },
    ];

    private static ImprovementPlanStep[] TagSteps(string facet) =>
    [
        new()
        {
            Ordinal = 1,
            ActionType = "Investigate",
            Title = "Inventory tagged signals",
            Description =
                "List every signal with this tag, including artifact hints and runs. Summarize dispositions to see whether the tag marks quality, process, or tooling debt.",
        },
        new()
        {
            Ordinal = 2,
            ActionType = "Policy",
            Title = "Assign owner and exit criteria",
            Description =
                "Record the responsible team, target date, and measurable exit criteria (for example, disposition mix or pilot checklist completion).",
        },
        new()
        {
            Ordinal = 3,
            ActionType = "UX",
            Title = "Apply targeted readability fixes",
            Description =
                "For tagged work affecting " + facet + ", apply the same readability, structure, and naming rules as other artifact themes.",
        },
        new()
        {
            Ordinal = 4,
            ActionType = "Verify",
            Title = "Review in triage",
            Description =
                "Present results in the standing triage forum. Archive the tag only after evidence counts drop below the agreed threshold.",
        },
    ];

    private static ImprovementPlanStep[] GenericSteps(string facet) =>
    [
        new()
        {
            Ordinal = 1,
            ActionType = "Investigate",
            Title = "Validate theme",
            Description = "Re-read the grouping explanation and canonical key. Confirm pilots still agree this bucket is actionable.",
        },
        new()
        {
            Ordinal = 2,
            ActionType = "Analyze",
            Title = "Define deliverables",
            Description = "List concrete files, templates, or policies that must change for " + facet + ".",
        },
        new()
        {
            Ordinal = 3,
            ActionType = "Verify",
            Title = "Measure after change",
            Description = "Capture before/after signal counts for the same window to prove the theme resolved.",
        },
    ];

    private static string Truncate(string value, int maxChars)
    {
        if (value.Length <= maxChars)
        
            return value;
        

        return value[..maxChars];
    }
}
