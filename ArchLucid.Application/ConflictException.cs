namespace ArchLucid.Application;

/// <summary>
///     Thrown when an operation conflicts with current state (e.g. duplicate, wrong phase).
///     Maps to HTTP 409 in the API layer.
/// </summary>
public sealed class ConflictException : InvalidOperationException
{
    /// <summary>Creates a conflict exception with the given message (maps to HTTP 409 in the API).</summary>
    /// <param name="message">Human-readable conflict description.</param>
    public ConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a conflict exception with an inner cause.</summary>
    /// <param name="message">Human-readable conflict description.</param>
    /// <param name="innerException">Underlying exception.</param>
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
