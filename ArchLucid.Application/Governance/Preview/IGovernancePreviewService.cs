using ArchiForge.Contracts.Governance.Preview;

namespace ArchiForge.Application.Governance.Preview;

/// <summary>
/// Read-only service that previews governance state changes without persisting anything.
/// Supports activation previews (what would change if a run's manifest were activated into an environment)
/// and environment-to-environment governance comparisons.
/// </summary>
public interface IGovernancePreviewService
{
    /// <summary>
    /// Returns a structured preview of how governance fields would change if the run's manifest
    /// were activated into the target environment.
    /// </summary>
    Task<GovernancePreviewResult> PreviewActivationAsync(
        GovernancePreviewRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares the governance state of two environments, returning the field-level differences between them.
    /// </summary>
    Task<GovernanceEnvironmentComparisonResult> CompareEnvironmentsAsync(
        GovernanceEnvironmentComparisonRequest request,
        CancellationToken cancellationToken = default);
}
