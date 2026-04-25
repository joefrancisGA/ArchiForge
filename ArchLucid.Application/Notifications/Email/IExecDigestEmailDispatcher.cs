using ArchLucid.Application.ExecDigest;

namespace ArchLucid.Application.Notifications.Email;

/// <summary>Sends the weekly executive digest using the transactional email stack (<see cref="IEmailTemplateRenderer"/>, <see cref="Core.Notifications.Email.IEmailProvider"/>, <see cref="Core.Notifications.ISentEmailLedger"/>).</summary>
public interface IExecDigestEmailDispatcher
{
    /// <summary>
    /// When the idempotency ledger rejects the key for this ISO week, returns <see langword="false"/> (duplicate / replay).
    /// </summary>
    Task<bool> TryDispatchAsync(
        Guid tenantId,
        string isoWeekIdempotencyKey,
        ExecDigestComposition composition,
        IReadOnlyList<string> toMailboxes,
        string unsubscribeAbsoluteUrl,
        CancellationToken cancellationToken);
}
