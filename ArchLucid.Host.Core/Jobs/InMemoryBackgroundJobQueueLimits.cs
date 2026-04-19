namespace ArchLucid.Host.Core.Jobs;

/// <summary>Public bounds for <see cref="InMemoryBackgroundJobQueue"/> so unit tests stay aligned with production caps.</summary>
public static class InMemoryBackgroundJobQueueLimits
{
    public const int MaxRetainedTerminalJobs = 200;
    public const int MaxPendingJobs = 500;
}
