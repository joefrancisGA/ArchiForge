namespace ArchiForge.Core.Diagnostics;

/// <summary>Thread-safe holder for the latest outbox gauge snapshot (read on Prometheus scrape).</summary>
public sealed class OutboxDepthGaugeState
{
    private readonly object _gate = new();
    private OutboxDepthGaugeValues _current;

    public OutboxDepthGaugeValues Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Publish(in OutboxDepthGaugeValues values)
    {
        lock (_gate)
        {
            _current = values;
        }
    }
}
