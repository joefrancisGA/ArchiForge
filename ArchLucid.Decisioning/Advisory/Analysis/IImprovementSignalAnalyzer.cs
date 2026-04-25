using ArchLucid.Core.Comparison;
using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Advisory.Services;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Advisory.Analysis;

/// <summary>
///     Derives structured <see cref="ImprovementSignal" /> items from a golden manifest, optional findings snapshot, and
///     optional run-to-run comparison.
/// </summary>
/// <remarks>
///     Consumed by <see cref="IImprovementAdvisorService" /> and <see cref="ImprovementAdvisorService" /> when building an
///     <see cref="ImprovementPlan" />.
/// </remarks>
public interface IImprovementSignalAnalyzer
{
    /// <summary>
    ///     Scans manifest sections (requirements, security, compliance, topology, cost, issues) and optional comparison
    ///     deltas.
    /// </summary>
    /// <param name="manifest">Current run golden manifest.</param>
    /// <param name="findingsSnapshot">Findings payload (reserved for future signal sources; may be ignored).</param>
    /// <param name="comparison">When set, adds regression/cost/decision-delta style signals.</param>
    /// <returns>Zero or more signals (not deduplicated across calls).</returns>
    IReadOnlyList<ImprovementSignal> Analyze(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        ComparisonResult? comparison = null);
}
