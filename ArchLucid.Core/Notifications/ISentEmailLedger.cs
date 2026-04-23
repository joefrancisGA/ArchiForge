namespace ArchLucid.Core.Notifications;

/// <summary>
///     Fail-closed idempotency: returns <see langword="false" /> when <paramref name="entry" />.IdempotencyKey
///     already exists.
/// </summary>
public interface ISentEmailLedger
{
    Task<bool> TryRecordSentAsync(SentEmailLedgerEntry entry, CancellationToken cancellationToken);
}
