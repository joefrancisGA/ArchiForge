namespace ArchiForge.Core.Scoping;

/// <summary>
/// Marks the current async flow as trusted SQL work that must bypass row-level security predicates
/// (schema bootstrap, data archival, migrations-style batch jobs). Pair with
/// configuration <c>SqlServer:RowLevelSecurity:ApplySessionContext</c> when rolling out RLS.
/// </summary>
public static class SqlRowLevelSecurityBypassAmbient
{
    private static readonly AsyncLocal<int> Depth = new();

    /// <summary>True while a <see cref="Enter"/> scope is active.</summary>
    public static bool IsActive => Depth.Value > 0;

    /// <summary>Increases bypass depth until disposed.</summary>
    public static IDisposable Enter()
    {
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
