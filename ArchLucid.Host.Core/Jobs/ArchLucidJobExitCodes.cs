namespace ArchLucid.Host.Core.Jobs;

/// <summary>Process exit codes for <see cref="ArchLucid.Jobs.Cli"/> (Container Apps Jobs / CI).</summary>
public static class ArchLucidJobExitCodes
{
    public const int Success = 0;

    public const int JobFailure = 1;

    public const int ConfigurationError = 2;

    public const int UnknownJob = 3;
}
