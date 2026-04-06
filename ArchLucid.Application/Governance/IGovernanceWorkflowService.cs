using ArchiForge.Contracts.Governance;

namespace ArchiForge.Application.Governance;

/// <summary>
/// Orchestrates the governance lifecycle for architecture manifests: approval request
/// submission, review (approve/reject), promotion between environments, and activation.
/// </summary>
public interface IGovernanceWorkflowService
{
    /// <summary>
    /// Creates and persists a new governance approval request for the specified run and manifest.
    /// </summary>
    /// <param name="runId">The run whose manifest is being submitted for approval.</param>
    /// <param name="manifestVersion">The specific manifest version to be reviewed.</param>
    /// <param name="sourceEnvironment">The environment the manifest is being promoted from.</param>
    /// <param name="targetEnvironment">The environment the manifest is being promoted to.</param>
    /// <param name="requestedBy">Identity of the user or system submitting the request.</param>
    /// <param name="requestComment">Optional comment explaining the reason for promotion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="GovernanceApprovalRequest"/>.</returns>
    /// <exception cref="RunNotFoundException">Thrown when <paramref name="runId"/> does not exist.</exception>
    Task<GovernanceApprovalRequest> SubmitApprovalRequestAsync(
        string runId,
        string manifestVersion,
        string sourceEnvironment,
        string targetEnvironment,
        string requestedBy,
        string? requestComment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an approval request as <c>Approved</c>.
    /// </summary>
    /// <param name="approvalRequestId">Identifier of the approval request to approve.</param>
    /// <param name="reviewedBy">Identity of the reviewer approving the request.</param>
    /// <param name="reviewComment">Optional comment from the reviewer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated <see cref="GovernanceApprovalRequest"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the request is not found, or is not in <c>Draft</c> or <c>Submitted</c> status.
    /// </exception>
    Task<GovernanceApprovalRequest> ApproveAsync(
        string approvalRequestId,
        string reviewedBy,
        string? reviewComment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an approval request as <c>Rejected</c>.
    /// </summary>
    /// <param name="approvalRequestId">Identifier of the approval request to reject.</param>
    /// <param name="reviewedBy">Identity of the reviewer rejecting the request.</param>
    /// <param name="reviewComment">Optional comment from the reviewer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated <see cref="GovernanceApprovalRequest"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the request is not found, or is not in <c>Draft</c> or <c>Submitted</c> status.
    /// </exception>
    Task<GovernanceApprovalRequest> RejectAsync(
        string approvalRequestId,
        string reviewedBy,
        string? reviewComment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a manifest promotion from one environment to another.
    /// Promotions to <c>prod</c> require a corresponding approved
    /// <see cref="GovernanceApprovalRequest"/>.
    /// </summary>
    /// <param name="runId">The run whose manifest is being promoted.</param>
    /// <param name="manifestVersion">The manifest version being promoted.</param>
    /// <param name="sourceEnvironment">The environment being promoted from.</param>
    /// <param name="targetEnvironment">The environment being promoted to.</param>
    /// <param name="promotedBy">Identity of the user performing the promotion.</param>
    /// <param name="approvalRequestId">
    /// Required when <paramref name="targetEnvironment"/> is <c>prod</c>.
    /// Must reference an approved request for the same run and manifest version.
    /// </param>
    /// <param name="notes">Optional free-text notes recorded on the promotion record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="GovernancePromotionRecord"/>.</returns>
    /// <exception cref="RunNotFoundException">Thrown when <paramref name="runId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when promoting to prod without a valid approved request, or when the approval
    /// request does not match the supplied run/manifest/environment.
    /// </exception>
    Task<GovernancePromotionRecord> PromoteAsync(
        string runId,
        string manifestVersion,
        string sourceEnvironment,
        string targetEnvironment,
        string promotedBy,
        string? approvalRequestId,
        string? notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a manifest version in the target environment, deactivating any previously
    /// active activation for that environment within the same transaction.
    /// </summary>
    /// <param name="runId">The run whose manifest is being activated.</param>
    /// <param name="manifestVersion">The manifest version to activate.</param>
    /// <param name="environment">The environment in which the manifest becomes the active version.</param>
    /// <param name="activatedBy">Identity of the actor performing activation (typically from HTTP context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="GovernanceEnvironmentActivation"/>.</returns>
    /// <exception cref="RunNotFoundException">Thrown when <paramref name="runId"/> does not exist.</exception>
    Task<GovernanceEnvironmentActivation> ActivateAsync(
        string runId,
        string manifestVersion,
        string environment,
        string activatedBy,
        CancellationToken cancellationToken = default);
}
