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
///     <see cref="GovernanceEnvironmentActivationRepository.CreateAsync" /> in one <see cref="IsolationLevel.Serializable" />
///     transaction so <c>FK_GovernanceEnvironmentActivations_Tenants</c> sees the parent row on shared CI databases.
/// </summary>
internal sealed class TenantPrimingGovernanceEnvironmentActivationRepository : IGovernanceEnvironmentActivationRepository
{
    private readonly string _connectionString;
    private readonly GovernanceEnvironmentActivationRepository _inner;

    public TenantPrimingGovernanceEnvironmentActivationRepository(
        string connectionString,
        IScopeContextProvider scopeContextProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);

        _connectionString = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString.Trim());
        _inner = new GovernanceEnvironmentActivationRepository(
            new GovernanceContractScopeRlsBypassDbConnectionFactory(_connectionString),
            scopeContextProvider);
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        GovernanceEnvironmentActivation item,
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

        GovernanceContractScopeRlsBypassDbConnectionFactory factory = new(_connectionString);
        await using SqlConnection conn = (SqlConnection)await factory.CreateOpenConnectionAsync(cancellationToken);
        SqlTransaction? tran = null;

        try
        {
            tran = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            await SqlServerPersistenceFixture.MergeGovernanceContractTenantAsync(conn, tran, cancellationToken);
            await _inner.CreateAsync(item, cancellationToken, conn, tran);
            tran.Commit();
        }
        catch
        {
            tran?.Rollback();
            throw;
        }
        finally
        {
            tran?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        GovernanceEnvironmentActivation item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null) =>
        _inner.UpdateAsync(item, cancellationToken, connection, transaction);

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByEnvironmentAsync(string environment,
        CancellationToken cancellationToken = default) =>
        _inner.GetByEnvironmentAsync(environment, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByRunIdAsync(string runId,
        CancellationToken cancellationToken = default) =>
        _inner.GetByRunIdAsync(runId, cancellationToken);
}
