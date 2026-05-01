namespace ArchLucid.Core.Scoping;

/// <summary>
///     Marks the current async flow as trusted SQL work that must bypass row-level security predicates
///     (schema bootstrap, data archival, migrations-style batch jobs). Pair with
///     configuration <c>SqlServer:RowLevelSecurity:ApplySessionContext</c> when rolling out RLS.
/// </summary>
public static class SqlRowLevelSecurityBypassAmbient
{
    private static readonly AsyncLocal<int> Depth = new();
    private static Func<bool>? _breakGlassEnabled;
    private static Func<bool>? _strictBypassRequired;

    static SqlRowLevelSecurityBypassAmbient()
    {
        ConfigureBypassPolicy(() => true, () => false);
    }

    /// <summary>True while a <see cref="Enter" /> scope is active.</summary>
    public static bool IsActive => Depth.Value > 0;

    /// <summary>
    ///     Host wiring: when <paramref name="strictBypassRequired" /> returns true, <see cref="Enter" /> requires
    ///     <paramref name="breakGlassEnabled" /> to return true (env + configuration break-glass).
    /// </summary>
    public static void ConfigureBypassPolicy(Func<bool> breakGlassEnabled, Func<bool> strictBypassRequired)
    {
        ArgumentNullException.ThrowIfNull(breakGlassEnabled);
        ArgumentNullException.ThrowIfNull(strictBypassRequired);

        _breakGlassEnabled = breakGlassEnabled;
        _strictBypassRequired = strictBypassRequired;
    }

    /// <summary>Increases bypass depth until disposed.</summary>
    public static IDisposable Enter()
    {
        if (_strictBypassRequired?.Invoke() == true
            && (_breakGlassEnabled is null || !_breakGlassEnabled.Invoke()))

            throw new InvalidOperationException(
                "SQL RLS bypass is blocked: when SqlServer:RowLevelSecurity:ApplySessionContext is true, "
                + "SqlRowLevelSecurityBypassAmbient.Enter requires ARCHLUCID_ALLOW_RLS_BYPASS=true and ArchLucid:Persistence:AllowRlsBypass=true.");

        Depth.Value++;
        return new PopScope();
    }

    private sealed class PopScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Depth.Value = Math.Max(0, Depth.Value - 1);
        }
    }
}
