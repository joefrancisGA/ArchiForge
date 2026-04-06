namespace ArchiForge.Host.Core.Hosted;

/// <summary>
/// Stable per-process id used as <c>HolderInstanceId</c> in <c>dbo.HostLeaderLeases</c>.
/// </summary>
public sealed class HostInstanceIdentifier
{
    /// <summary>
    /// Creates an identifier unique to this OS process (survives the lifetime of the host).
    /// </summary>
    public HostInstanceIdentifier()
        : this($"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}")
    {
    }

    /// <summary>
    /// Test-only deterministic id.
    /// </summary>
    public static HostInstanceIdentifier ForTests(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new HostInstanceIdentifier(value);
    }

    private HostInstanceIdentifier(string value)
    {
        Value = value;
    }

    /// <summary>Opaque string stored in SQL (max 256 in schema).</summary>
    public string Value { get; }
}
