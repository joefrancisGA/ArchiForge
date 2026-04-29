namespace ArchLucid.Core;

/// <summary>Thrown when a tenant exceeds configured LLM token quota for the current window or UTC day.</summary>
public sealed class LlmTokenQuotaExceededException(string message, DateTimeOffset? retryAfterUtc = null)
    : InvalidOperationException(message)
{
    /// <summary>
    ///     Earliest UTC instant when a retry may succeed (sliding-window expiry or next UTC-day budget); <see langword="null" />
    ///     when not computed.
    /// </summary>
    public DateTimeOffset? RetryAfterUtc { get; } = retryAfterUtc;
}
