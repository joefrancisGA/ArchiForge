using ArchLucid.Contracts.Architecture;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// Computes the proof-of-ROI <see cref="PilotRunDeltas"/> for a single architecture run by joining read-only
/// projections from existing services (<see cref="IRunDetailQueryService"/>, audit repository, agent execution
/// trace repository, finding evidence chain). Both <see cref="FirstValueReportBuilder"/> and
/// <see cref="SponsorOnePagerPdfBuilder"/> consume this so the Markdown sibling and the PDF wrapper render
/// identical numbers.
/// </summary>
public interface IPilotRunDeltaComputer
{
    /// <summary>
    /// Returns the computed deltas for <paramref name="detail"/>. When no findings exist, the evidence-chain fields
    /// stay <see langword="null"/>; when the audit row count cannot be queried, <c>AuditRowCount</c> is <c>0</c>
    /// (the caller still renders the line so the structure of the report does not shift between runs).
    /// </summary>
    Task<PilotRunDeltas> ComputeAsync(
        ArchitectureRunDetail detail,
        CancellationToken cancellationToken = default);
}
