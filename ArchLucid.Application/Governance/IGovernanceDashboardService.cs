using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Builds read-only governance dashboard aggregates for a tenant scope.
/// </summary>
public interface IGovernanceDashboardService
{
    Task<GovernanceDashboardSummary> GetDashboardAsync(
        Guid tenantId,
        int maxPending = 20,
        int maxDecisions = 20,
        int maxChanges = 20,
        CancellationToken cancellationToken = default);
}
