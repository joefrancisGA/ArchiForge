namespace ArchLucid.TestSupport.GoldenCorpus;

/// <summary><see cref="TimeProvider" /> pinned to a single UTC instant for deterministic golden outputs.</summary>
public sealed class FrozenUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private readonly DateTimeOffset _utcNow = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}
