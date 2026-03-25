namespace ArchiForge.Api.Services;

public sealed class ReplayDiagnosticsRecorder : IReplayDiagnosticsRecorder
{
    private readonly int _capacity;
    private readonly Queue<ReplayDiagnosticsEntry> _recent;
    private readonly Lock _lock = new();

    public ReplayDiagnosticsRecorder(IConfiguration configuration)
    {
        int configured = configuration.GetValue("ReplayDiagnostics:Capacity", 100);
        _capacity = configured is > 0 and <= 1000 ? configured : 100;
        _recent = new Queue<ReplayDiagnosticsEntry>(_capacity);
    }

    public void Record(ReplayDiagnosticsEntry entry)
    {
        lock (_lock)
        {
            while (_recent.Count >= _capacity)
                _recent.Dequeue();
            _recent.Enqueue(entry);
        }
    }

    public IReadOnlyList<ReplayDiagnosticsEntry> GetRecent(int maxCount = 100)
    {
        lock (_lock)
        {
            ReplayDiagnosticsEntry[] list = _recent.ToArray();
            if (list.Length <= maxCount)
                return list;
            return list.Skip(list.Length - maxCount).ToArray();
        }
    }
}
