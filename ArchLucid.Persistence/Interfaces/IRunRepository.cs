using System.Data;

using ArchLucid.Contracts.Common;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Interfaces;

// ReSharper disable InvalidXmlDocComment
/// <summary>
///     Persistence contract for <see cref="RunRecord" /> — the root authority run entity that
///     anchors every snapshot, manifest, trace, and artifact bundle for an architecture run.
/// </summary>
/// <remarks>
///     The optional <paramref name="connection" /> and <paramref name="transaction" /> overloads on write
///     methods allow callers to enlist the operation inside an existing ambient transaction.
///     When omitted, each method opens its own connection.
/// </remarks>
/// // ReSharper enable InvalidXmlDocComment
public interface IRunRepository
{
    /// <summary>
    ///     Inserts or replaces the <see cref="RunRecord" />. Callers may pass an existing
    ///     <paramref name="connection" /> and <paramref name="transaction" /> to participate
    ///     in a multi-statement transaction.
    /// </summary>
    /// <param name="run">The run to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Returns the run with the given <paramref name="runId" /> within <paramref name="scope" />,
    ///     or <see langword="null" /> when not found or outside the caller's scope.
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary for the lookup.</param>
    /// <param name="runId">Primary key of the run.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct);

    /// <summary>
    ///     Returns up to <paramref name="take" /> runs for <paramref name="projectId" /> within
    ///     <paramref name="scope" />, ordered by <c>CreatedUtc</c> descending (newest first).
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary for the query.</param>
    /// <param name="projectId">Project slug or identifier to filter by.</param>
    /// <param name="take">Maximum number of rows to return.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<RunRecord>> ListByProjectAsync(ScopeContext scope, string projectId, int take,
        CancellationToken ct);

    /// <summary>
    ///     Stable keyset page of runs for <paramref name="projectId" /> within <paramref name="scope" />, newest first.
    ///     Pass both <paramref name="cursorCreatedUtc" /> and <paramref name="cursorRunId" /> after the last item of the
    ///     previous page; both <see langword="null" /> means the first page (no OFFSET).
    /// </summary>
    Task<RunListPage> ListByProjectKeysetAsync(
        ScopeContext scope,
        string projectId,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct);

    /// <summary>
    ///     Returns up to <paramref name="take" /> runs in <paramref name="scope" /> (all project slugs), ordered by
    ///     <c>CreatedUtc</c> descending. Excludes archived rows.
    /// </summary>
    Task<IReadOnlyList<RunRecord>> ListRecentInScopeAsync(ScopeContext scope, int take, CancellationToken ct);

    /// <summary>
    ///     Stable keyset page within <paramref name="scope" /> (all project slugs), newest first — see
    ///     <see cref="ListByProjectKeysetAsync" /> for cursor semantics.
    /// </summary>
    Task<RunListPage> ListRecentInScopeKeysetAsync(
        ScopeContext scope,
        DateTime? cursorCreatedUtc,
        Guid? cursorRunId,
        int take,
        CancellationToken ct);

    /// <summary>
    ///     Applies an update to an existing run row. Callers may pass an existing
    ///     <paramref name="connection" /> and <paramref name="transaction" /> to participate
    ///     in a multi-statement transaction.
    /// </summary>
    /// <param name="run">The run with updated field values.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task UpdateAsync(
        RunRecord run,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Sets <see cref="RunRecord.ArchivedUtc" /> for runs with <c>CreatedUtc</c> strictly before
    ///     <paramref name="cutoffUtc" />
    ///     that are not yet archived. Returns the count and scope keys for each archived row (for cache eviction).
    /// </summary>
    Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct);

    /// <summary>
    ///     Soft-archives up to 100 runs by primary key. Missing or already-archived ids are reported in
    ///     <see cref="RunArchiveByIdsResult.Failed" /> without failing the whole operation.
    /// </summary>
    Task<RunArchiveByIdsResult> ArchiveRunsByIdsAsync(IReadOnlyList<Guid> runIds, CancellationToken ct);

    /// <summary>
    ///     Counts runs in <paramref name="scope" /> for <paramref name="architectureRequestId" /> that are not archived
    ///     and whose <see cref="RunRecord.LegacyRunStatus" /> is not terminal (<see cref="ArchitectureRunStatus.Committed" />
    ///     or <see cref="ArchitectureRunStatus.Failed" />).
    /// </summary>
    Task<int> CountActiveRunsForArchitectureRequestAsync(
        ScopeContext scope,
        string architectureRequestId,
        CancellationToken ct);
}
