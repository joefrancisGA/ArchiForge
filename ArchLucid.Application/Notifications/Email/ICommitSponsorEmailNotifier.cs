namespace ArchLucid.Application.Notifications.Email;

/// <summary>
///     Sends an optional sponsor heads-up email after a successful manifest commit, using the tenant
///     trial admin mailbox when that address is available.
/// </summary>
public interface ICommitSponsorEmailNotifier
{
    /// <summary>
    ///     Best-effort notification: never throws; logs and returns when mail cannot be sent.
    /// </summary>
    Task NotifyAfterCommitAsync(Guid tenantId, string runId, CancellationToken cancellationToken);
}
