using System.Data;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Re-primes <c>dbo.Tenants</c> before <see cref="IGovernanceEnvironmentActivationRepository.CreateAsync" /> when not
///     joining an external connection, matching <see cref="TenantPrimingGovernanceApprovalRequestRepository" /> for shared CI catalogs.
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

        _connectionString = connectionString;
        _inner = new GovernanceEnvironmentActivationRepository(
            new RlsBypassTestDbConnectionFactory(connectionString),
            scopeContextProvider);
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        GovernanceEnvironmentActivation item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        if (connection is null)
            await SqlServerPersistenceFixture.PrimeGovernanceContractTenantAsync(_connectionString, cancellationToken);

        await _inner.CreateAsync(item, cancellationToken, connection, transaction);
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
