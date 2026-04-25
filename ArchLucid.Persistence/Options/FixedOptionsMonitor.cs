using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Options;

/// <summary>
///     Constant <see cref="IOptionsMonitor{TOptions}" /> for hosts that do not use the options configuration pipeline
///     (e.g. CLI tools).
/// </summary>
public sealed class FixedOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
    public FixedOptionsMonitor(TOptions currentValue)
    {
        CurrentValue = currentValue ?? throw new ArgumentNullException(nameof(currentValue));
    }

    public TOptions CurrentValue
    {
        get;
    }

    public TOptions Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        return NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        internal static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
