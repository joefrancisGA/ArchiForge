using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Compares stakeholder explanation text to persisted <see cref="ExplainabilityTrace" /> data (heuristic).
/// </summary>
public interface IExplanationFaithfulnessChecker
{
    /// <summary>
    ///     When <paramref name="snapshot" /> is null or has no findings, returns a vacuous report with
    ///     <see cref="ExplanationFaithfulnessReport.SupportRatio" /> 1.0.
    /// </summary>
    ExplanationFaithfulnessReport CheckFaithfulness(ExplanationResult explanation, FindingsSnapshot? snapshot);
}
