using System.Data;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="SqlServerPersistenceFixture.MergeGovernanceContractTenantAsync" /> and
///     <see cref="GovernanceApprovalRequestRepository.CreateAsync" /> in one <see cref="System.Data.IsolationLevel.Serializable" />
///     transaction so <c>FK_GovernanceApprovalRequests_Tenants</c> sees the parent row on shared CI databases.
/// </summary>
internal sealed class TenantPrimingGovernanceApprovalRequestRepository : IGovernanceApprovalRequestRepository
{
    private readonly string _connectionString;
    private readonly GovernanceApprovalRequestRepository _inner;

    public TenantPrimingGovernanceApprovalRequestRepository(
        string connectionString,
        IScopeContextProvider scopeContextProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);

        _connectionString = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString.Trim());
        _inner = new GovernanceApprovalRequestRepository(
            new RlsBypassTestDbConnectionFactory(_connectionString),
            scopeContextProvider);
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        GovernanceApprovalRequest item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        if (connection is not null)
        {
            await _inner.CreateAsync(item, cancellationToken, connection, transaction);

            return;
        }

        RlsBypassTestDbConnectionFactory factory = new(_connectionString);
        await using SqlConnection conn = (SqlConnection)await factory.CreateOpenConnectionAsync(cancellationToken);
        await using SqlTransaction tran =
            (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            await SqlServerPersistenceFixture.MergeGovernanceContractTenantAsync(conn, tran, cancellationToken);
            await _inner.CreateAsync(item, cancellationToken, conn, tran);
            await tran.CommitAsync(cancellationToken);
        }
        catch
        {
            await tran.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> TryTransitionFromReviewableAsync(
        string approvalRequestId,
        string newStatus,
        string reviewedBy,
        string? reviewedByActorKey,
        string? reviewComment,
        DateTime reviewedUtc,
        CancellationToken cancellationToken = default) =>
        _inner.TryTransitionFromReviewableAsync(
            approvalRequestId,
            newStatus,
            reviewedBy,
            reviewedByActorKey,
            reviewComment,
            reviewedUtc,
            cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default) =>
        _inner.UpdateAsync(item, cancellationToken);

    /// <inheritdoc />
    public Task<GovernanceApprovalRequest?> GetByIdAsync(string approvalRequestId,
        CancellationToken cancellationToken = default) =>
        _inner.GetByIdAsync(approvalRequestId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(string runId,
        CancellationToken cancellationToken = default) =>
        _inner.GetByRunIdAsync(runId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingAsync(int maxRows = 50,
        CancellationToken cancellationToken = default) =>
        _inner.GetPendingAsync(maxRows, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetRecentDecisionsAsync(int maxRows = 50,
        CancellationToken cancellationToken = default) =>
        _inner.GetRecentDecisionsAsync(maxRows, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingSlaBreachedAsync(DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        _inner.GetPendingSlaBreachedAsync(utcNow, cancellationToken);

    /// <inheritdoc />
    public Task PatchSlaBreachNotifiedAsync(string approvalRequestId, DateTime slaBreachNotifiedUtc,
        CancellationToken cancellationToken = default) =>
        _inner.PatchSlaBreachNotifiedAsync(approvalRequestId, slaBreachNotifiedUtc, cancellationToken);
}
