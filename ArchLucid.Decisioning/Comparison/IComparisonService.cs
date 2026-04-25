using ArchLucid.Core.Comparison;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Comparison;

/// <summary>
///     Pure domain comparison of two <see cref="GoldenManifest" /> snapshots into a structured
///     <see cref="ComparisonResult" /> (decisions, requirements, security, topology, cost).
/// </summary>
/// <remarks>
///     Default implementation: <see cref="ComparisonService" /> (registered singleton in API). Differs from persistence
///     <c>IAuthorityCompareService</c>, which emits flat <c>DiffItem</c> lists for authority UI.
///     Primary callers: <c>ComparisonController</c>, <c>AdvisoryScanRunner</c>, <c>AdvisoryController</c>, alert
///     simulation context, ask/export flows.
/// </remarks>
public interface IComparisonService
{
    /// <summary>
    ///     Compares <paramref name="baseManifest" /> → <paramref name="targetManifest" /> and fills change collections plus
    ///     <see cref="ComparisonResult.SummaryHighlights" />.
    /// </summary>
    /// <param name="baseManifest">
    ///     Earlier or baseline manifest (its <see cref="GoldenManifest.RunId" /> becomes
    ///     <see cref="ComparisonResult.BaseRunId" />).
    /// </param>
    /// <param name="targetManifest">
    ///     Later or candidate manifest (<see cref="GoldenManifest.RunId" /> →
    ///     <see cref="ComparisonResult.TargetRunId" />).
    /// </param>
    /// <returns>Non-null result; empty delta lists mean no changes in the compared sections.</returns>
    /// <remarks>
    ///     Does not persist; safe to call on in-memory manifests. Not scope-aware—callers must enforce
    ///     tenant/workspace/project if needed.
    /// </remarks>
    ComparisonResult Compare(GoldenManifest baseManifest, GoldenManifest targetManifest);
}
