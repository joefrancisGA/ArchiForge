using ArchiForge.Host.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Core.Services;

public sealed class ReplayDiagnosticsRecorder(IOptionsMonitor<ReplayDiagnosticsOptions> optionsMonitor)
    : IReplayDiagnosticsRecorder
{
    private readonly IOptionsMonitor<ReplayDiagnosticsOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly Queue<ReplayDiagnosticsEntry> _recent = new();
    private readonly Lock _lock = new();

    public void Record(ReplayDiagnosticsEntry entry)
    {
        ReplayDiagnosticsOptions opts = _optionsMonitor.CurrentValue;
        int capacity = opts.Capacity is > 0 and <= 1000 ? opts.Capacity : 100;
        int retentionMinutes = Math.Max(0, opts.RetentionMinutes);
        DateTime cutoffUtc = retentionMinutes > 0
            ? DateTime.UtcNow.AddMinutes(-retentionMinutes)
            : DateTime.MinValue;

        lock (_lock)
        {
            while (_recent.TryPeek(out ReplayDiagnosticsEntry? head) && head.TimestampUtc < cutoffUtc)
                _recent.Dequeue();

            while (_recent.Count >= capacity)
                _recent.Dequeue();

            _recent.Enqueue(entry);
        }
    }

    public IReadOnlyList<ReplayDiagnosticsEntry> GetRecent(int maxCount = 100)
    {
        ReplayDiagnosticsOptions opts = _optionsMonitor.CurrentValue;
        int retentionMinutes = Math.Max(0, opts.RetentionMinutes);
        DateTime cutoffUtc = retentionMinutes > 0
            ? DateTime.UtcNow.AddMinutes(-retentionMinutes)
            : DateTime.MinValue;

        lock (_lock)
        {
            IEnumerable<ReplayDiagnosticsEntry> seq = _recent.Where(e => e.TimestampUtc >= cutoffUtc);

            ReplayDiagnosticsEntry[] list = seq.ToArray();

            return list.Length <= maxCount ? list : list.Skip(list.Length - maxCount).ToArray();
        }
    }
}
