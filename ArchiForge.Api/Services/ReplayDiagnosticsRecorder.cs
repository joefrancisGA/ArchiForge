namespace ArchiForge.Api.Services;

public sealed class ReplayDiagnosticsRecorder : IReplayDiagnosticsRecorder
{
    private const int DefaultCapacity = 100;
    private readonly Queue<ReplayDiagnosticsEntry> _recent = new(DefaultCapacity);
    private readonly object _lock = new();

    public void Record(ReplayDiagnosticsEntry entry)
    {
        if (entry == null) return;

        lock (_lock)
        {
            while (_recent.Count >= DefaultCapacity)
                _recent.Dequeue();
            _recent.Enqueue(entry);
        }
    }

    public IReadOnlyList<ReplayDiagnosticsEntry> GetRecent(int maxCount = 100)
    {
        lock (_lock)
        {
            var list = _recent.ToArray();
            if (list.Length <= maxCount) return list;
            return list.Skip(list.Length - maxCount).ToArray();
        }
    }
}
