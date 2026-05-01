using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Builds time-bucketed aggregates of policy pack change log activity for a tenant.
/// </summary>
public interface IComplianceDriftTrendService
{
    Task<IReadOnlyList<ComplianceDriftTrendPoint>> GetTrendAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        TimeSpan bucketSize,
        CancellationToken cancellationToken = default);
}
