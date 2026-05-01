using ArchLucid.Decisioning.Configuration;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Sets <see cref="Finding.HumanReviewStatus" /> on new snapshots using deterministic-trace markers and config.
/// </summary>
public static class FindingHumanReviewInitializer
{
    /// <summary>
    ///     Marks findings for review when they are not rule-deterministic and severity is Critical/Error, when enabled, or
    ///     when <see cref="HumanReviewFindingOptions.RequiredFindingTypes" /> matches.
    /// </summary>
    public static void Apply(IReadOnlyList<Finding> findings, HumanReviewFindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(options);

        HashSet<string> types = new(StringComparer.OrdinalIgnoreCase);

        foreach (string t in options.RequiredFindingTypes)
        {
            if (!string.IsNullOrWhiteSpace(t))
                types.Add(t.Trim());
        }

        foreach (Finding f in findings)
        {
            if (f.HumanReviewStatus is not FindingHumanReviewStatus.NotRequired)
                continue;

            bool deterministic = f.Trace.AlternativePathsConsidered.Any(static p =>
                string.Equals(p, ExplainabilityTraceMarkers.RuleBasedDeterministicSinglePathNote,
                    StringComparison.Ordinal));

            bool severityTriggers = options.RequireForCriticalOrErrorWhenNotDeterministic
                                    && !deterministic
                                    && f.Severity is FindingSeverity.Critical or FindingSeverity.Error;

            bool typeTriggers = types.Contains(f.FindingType);

            if (!severityTriggers && !typeTriggers)
                continue;

            f.HumanReviewStatus = FindingHumanReviewStatus.Pending;
        }
    }
}
