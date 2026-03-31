using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Interfaces;

// ReSharper disable InvalidXmlDocComment
/// <summary>
/// Persistence contract for <see cref="RunRecord"/> — the root authority run entity that
/// anchors every snapshot, manifest, trace, and artifact bundle for an architecture run.
/// </summary>
/// <remarks>
/// The optional <paramref name="connection"/> and <paramref name="transaction"/> overloads on write
/// methods allow callers to enlist the operation inside an existing ambient transaction.
/// When omitted, each method opens its own connection.
/// </remarks>
/// // ReSharper enable InvalidXmlDocComment
public interface IRunRepository
{
    /// <summary>
    /// Inserts or replaces the <see cref="RunRecord"/>. Callers may pass an existing
    /// <paramref name="connection"/> and <paramref name="transaction"/> to participate
    /// in a multi-statement transaction.
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
    /// Returns the run with the given <paramref name="runId"/> within <paramref name="scope"/>,
    /// or <see langword="null"/> when not found or outside the caller's scope.
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary for the lookup.</param>
    /// <param name="runId">Primary key of the run.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<RunRecord?> GetByIdAsync(ScopeContext scope, Guid runId, CancellationToken ct);

    /// <summary>
    /// Returns up to <paramref name="take"/> runs for <paramref name="projectId"/> within
    /// <paramref name="scope"/>, ordered by <c>CreatedUtc</c> descending (newest first).
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary for the query.</param>
    /// <param name="projectId">Project slug or identifier to filter by.</param>
    /// <param name="take">Maximum number of rows to return.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<RunRecord>> ListByProjectAsync(ScopeContext scope, string projectId, int take, CancellationToken ct);

    /// <summary>
    /// Applies an update to an existing run row. Callers may pass an existing
    /// <paramref name="connection"/> and <paramref name="transaction"/> to participate
    /// in a multi-statement transaction.
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
    /// Sets <see cref="RunRecord.ArchivedUtc"/> for runs with <c>CreatedUtc</c> strictly before <paramref name="cutoffUtc"/>
    /// that are not yet archived. Returns the number of rows updated.
    /// </summary>
    Task<int> ArchiveRunsCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct);
}
