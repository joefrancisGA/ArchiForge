using System.Data;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Interfaces;

public interface IDecisionTraceRepository
{
    Task SaveAsync(
        DecisionTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<DecisionTrace?> GetByIdAsync(Guid decisionTraceId, CancellationToken ct);
}

