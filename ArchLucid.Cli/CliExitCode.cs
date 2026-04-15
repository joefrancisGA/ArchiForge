namespace ArchLucid.Cli;

/// <summary>
/// Process exit codes for <c>archlucid</c>. Use with <c>--json</c> (leading global flag) for machine-readable stderr payloads.
/// </summary>
public static class CliExitCode
{
    /// <summary>Command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Missing or invalid invocation (wrong arity, unknown top-level command).</summary>
    public const int UsageError = 1;

    /// <summary>Invalid API base URL or related configuration (see stderr / JSON <c>message</c>).</summary>
    public const int ConfigurationError = 2;

    /// <summary>API host not reachable or health probe failed (transport / service down).</summary>
    public const int ApiUnavailable = 3;

    /// <summary>Command ran but the operation failed (HTTP error, validation, filesystem, readiness after connect, etc.).</summary>
    public const int OperationFailed = 4;
}
