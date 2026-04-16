using System.Collections.Concurrent;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
/// Bounded in-process <see cref="SemaphoreSlim"/> map for create-run idempotency (evicts idle entries under pressure).
/// </summary>
internal sealed class RunCreateIdempotencyGateCache
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    private readonly int _capacity;

    private readonly TimeSpan _idleTtl;

    public RunCreateIdempotencyGateCache(int capacity = 10_000, TimeSpan? idleTtl = null)
    {
        _capacity = capacity > 0 ? capacity : 10_000;
        _idleTtl = idleTtl ?? TimeSpan.FromMinutes(5);
    }

    public SemaphoreSlim GetOrAddGate(string key)
    {
        long ticks = Environment.TickCount64;

        Entry entry = _entries.AddOrUpdate(
            key,
            _ => new Entry(new SemaphoreSlim(1, 1), ticks),
            (_, existing) =>
            {
                existing.LastUsedTicks = ticks;

                return existing;
            });

        return entry.Gate;
    }

    public void TryEvictAfterRelease(string releasedKey)
    {
        if (_entries.Count <= _capacity)
            return;

        long nowTicks = Environment.TickCount64;
        long ttlMs = (long)_idleTtl.TotalMilliseconds;

        foreach (KeyValuePair<string, Entry> pair in _entries)
        {
            if (_entries.Count <= _capacity)
                break;

            Entry e = pair.Value;

            if (pair.Key == releasedKey)
                continue;

            if (e.Gate.CurrentCount == 0)
                continue;

            if (nowTicks - e.LastUsedTicks > ttlMs
                && _entries.TryRemove(pair.Key, out Entry? removed))
            {
                removed.Gate.Dispose();
            }
        }
    }

    private sealed class Entry(SemaphoreSlim gate, long lastUsedTicks)
    {
        public SemaphoreSlim Gate { get; } = gate;

        public long LastUsedTicks = lastUsedTicks;
    }
}
