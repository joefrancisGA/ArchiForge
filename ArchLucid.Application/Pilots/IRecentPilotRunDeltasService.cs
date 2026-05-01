using ArchLucid.Contracts.Pilots;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Reads the most recent committed runs in the current scope and projects them as
///     a slim aggregated <see cref="RecentPilotRunDeltasResponse" /> for the operator shell's
///     <c>BeforeAfterDeltaPanel</c> (top, sidebar, inline-prior-run-lookup variants).
/// </summary>
/// <remarks>
///     Uses <see cref="IRunDetailQueryService" /> + <see cref="IPilotRunDeltaComputer" /> so the same
///     numbers a sponsor sees in the per-run value report appear in the aggregate. No new persistence path.
/// </remarks>
public interface IRecentPilotRunDeltasService
{
    /// <summary>Hard floor — a single run still produces a meaningful "median" (= the value itself).</summary>
    public const int MinCount = 1;

    /// <summary>
    ///     Hard ceiling — the panel is meant for "headline" framing, not exhaustive listing. Above this the
    ///     dedicated runs-list page is the right surface, and the per-run delta computation cost dominates.
    /// </summary>
    public const int MaxCount = 25;

    /// <summary>Default when the caller omits <c>?count=</c> on the HTTP surface.</summary>
    public const int DefaultCount = 5;

    /// <summary>
    ///     Returns the most recent <paramref name="count" /> committed runs in scope (newest first), with
    ///     server-computed median aggregates over <c>TotalFindings</c> and <c>TimeToCommittedManifest</c>.
    ///     <paramref name="count" /> is clamped to <see cref="MinCount" />..<see cref="MaxCount" />.
    /// </summary>
    Task<RecentPilotRunDeltasResponse> GetRecentDeltasAsync(
        int count,
        CancellationToken cancellationToken = default);
}
