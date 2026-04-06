namespace ArchiForge.Host.Core.Configuration;

/// <summary>
/// In-memory ring buffer for comparison replay diagnostics (<see cref="Services.IReplayDiagnosticsRecorder"/>).
/// </summary>
public sealed class ReplayDiagnosticsOptions
{
    public const string SectionName = "ReplayDiagnostics";

    /// <summary>Maximum entries retained (clamped to 1–1000; invalid values default to 100).</summary>
    public int Capacity { get; set; } = 100;

    /// <summary>
    /// Entries older than this many minutes are evicted from the front of the queue on each <see cref="Services.IReplayDiagnosticsRecorder.Record"/>.
    /// <c>0</c> disables time-based eviction (capacity-only trimming).
    /// </summary>
    public int RetentionMinutes { get; set; } = 1440;
}
