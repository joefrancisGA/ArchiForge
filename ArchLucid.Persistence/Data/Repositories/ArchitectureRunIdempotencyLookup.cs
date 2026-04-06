namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Stored mapping from HTTP scope + idempotency key hash to a created architecture run.
/// </summary>
public sealed class ArchitectureRunIdempotencyLookup
{
    /// <summary>Architecture run id (hex, no dashes).</summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>SHA-256 of the canonical request JSON used when the row was created.</summary>
    public byte[] RequestFingerprint { get; init; } = [];
}
