using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Alerts;

namespace ArchLucid.Decisioning.Advisory.Scheduling;

/// <summary>
///     Renders an <see cref="ArchitectureDigest" /> from an <see cref="ImprovementPlan" /> and optional evaluated
///     <see cref="AlertRecord" /> list.
/// </summary>
public interface IArchitectureDigestBuilder
{
    /// <summary>
    ///     Builds markdown content (top recommendations, summary notes, alert section) and populates
    ///     <see cref="ArchitectureDigest.MetadataJson" /> with recommendation/alert counts.
    /// </summary>
    /// <param name="tenantId">Digest scope.</param>
    /// <param name="workspaceId">Digest scope.</param>
    /// <param name="projectId">Digest scope.</param>
    /// <param name="runId">Optional authority run the scan targeted.</param>
    /// <param name="comparedToRunId">Optional baseline run when the plan was diff-based.</param>
    /// <param name="plan">Advisory plan whose recommendations and summary notes feed the digest.</param>
    /// <param name="evaluatedAlerts">Optional alerts evaluated during the same scan; included in markdown and metadata counts.</param>
    /// <returns>New digest instance with a fresh <see cref="ArchitectureDigest.DigestId" />.</returns>
    ArchitectureDigest Build(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId,
        ImprovementPlan plan,
        IReadOnlyList<AlertRecord>? evaluatedAlerts = null);
}
