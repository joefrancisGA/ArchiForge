using System.Data;

using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Core.Scoping;

namespace ArchiForge.Decisioning.Interfaces;

/// <summary>
/// Persistence contract for <see cref="DecisionTrace"/> records (rule audit) that capture the
/// full rule-application log produced by the decision engine during a run.
/// </summary>
public interface IDecisionTraceRepository
{
    /// <summary>
    /// Persists a decision trace. Callers may pass an existing <paramref name="connection"/>
    /// and <paramref name="transaction"/> to participate in a multi-statement transaction.
    /// </summary>
    /// <param name="trace">The trace to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        DecisionTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns the decision trace with the given <paramref name="decisionTraceId"/> within
    /// <paramref name="scope"/>, or <see langword="null"/> when not found or outside the
    /// caller's scope.
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary enforced by the implementation.</param>
    /// <param name="decisionTraceId">Primary key of the trace.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<DecisionTrace?> GetByIdAsync(ScopeContext scope, Guid decisionTraceId, CancellationToken ct);
}

