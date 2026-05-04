using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Re-primes <c>dbo.Tenants</c> before each <see cref="IGovernancePromotionRecordRepository.CreateAsync" /> so shared
///     CI databases keep the FK parent for <c>GovernancePromotionRecords</c>.
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

        _connectionString = connectionString;
        _inner = new GovernancePromotionRecordRepository(
            new RlsBypassTestDbConnectionFactory(connectionString),
            scopeContextProvider);
    }

    /// <inheritdoc />
    public async Task CreateAsync(GovernancePromotionRecord item, CancellationToken cancellationToken = default)
    {
        await SqlServerPersistenceFixture.PrimeGovernanceContractTenantAsync(_connectionString, cancellationToken);

        await _inner.CreateAsync(item, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(string runId,
        CancellationToken cancellationToken = default) =>
        _inner.GetByRunIdAsync(runId, cancellationToken);
}
