using System.Data;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Data.Repositories;

using ArchLucid.Persistence.Tests;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="SqlServerPersistenceFixture.MergeGovernanceContractTenantAsync" /> and
///     <see cref="GovernancePromotionRecordRepository.CreateAsync" /> in one <see cref="IsolationLevel.Serializable" />
///     transaction so <c>FK_GovernancePromotionRecords_Tenants</c> sees the parent row on shared CI databases.
/// </summary>
internal sealed class TenantPrimingGovernancePromotionRecordRepository : IGovernancePromotionRecordRepository
{
    private readonly string _connectionString;
    private readonly GovernancePromotionRecordRepository _inner;

    public TenantPrimingGovernancePromotionRecordRepository(
        string connectionString,
        IScopeContextProvider scopeContextProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);

        _connectionString = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString.Trim());
        _inner = new GovernancePromotionRecordRepository(
            new GovernanceContractScopeRlsBypassDbConnectionFactory(_connectionString),
            scopeContextProvider);
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        GovernancePromotionRecord item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        if (connection is not null)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            await SqlServerPersistenceFixture.MergeGovernanceContractTenantAsync(connection, transaction, cancellationToken);
            await _inner.CreateAsync(item, cancellationToken, connection, transaction);

            return;
        }

        await SqlServerPersistenceFixture.PrimeGovernanceContractTenantAsync(_connectionString, cancellationToken);

        GovernanceContractScopeRlsBypassDbConnectionFactory factory = new(_connectionString);
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
    public Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(string runId,
        CancellationToken cancellationToken = default) =>
        _inner.GetByRunIdAsync(runId, cancellationToken);
}
