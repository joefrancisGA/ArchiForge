namespace ArchLucid.Application.Traceability;

/// <summary>Thrown when a traceability ZIP would exceed the configured byte cap.</summary>
public sealed class TraceabilityBundleTooLargeException : InvalidOperationException
{
    public TraceabilityBundleTooLargeException(long attemptedBytes, long maxBytes)
        : base($"Traceability bundle size {attemptedBytes} exceeds cap {maxBytes}.")
    {
        AttemptedBytes = attemptedBytes;
        MaxBytes = maxBytes;
    }

    public long AttemptedBytes
    {
        get;
    }

    public long MaxBytes
    {
        get;
    }
}
