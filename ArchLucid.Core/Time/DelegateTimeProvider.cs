namespace ArchLucid.Core.Time;

/// <summary>Test or bridge <see cref="TimeProvider"/> backed by a delegate returning UTC wall clock.</summary>
internal sealed class DelegateTimeProvider : TimeProvider
{
    private readonly Func<DateTimeOffset> _getUtcNow;

    public DelegateTimeProvider(Func<DateTimeOffset> getUtcNow)
    {
        _getUtcNow = getUtcNow ?? throw new ArgumentNullException(nameof(getUtcNow));
    }

    public override DateTimeOffset GetUtcNow() => _getUtcNow();
}
