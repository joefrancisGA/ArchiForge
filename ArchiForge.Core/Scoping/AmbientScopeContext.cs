namespace ArchiForge.Core.Scoping;

/// <summary>
/// Optional ambient scope for non-HTTP pipelines (e.g. advisory background scans) so scoped services
/// such as <see cref="IScopeContextProvider"/> implementations can resolve the same tenant/workspace/project
/// as the job without an <c>HttpContext</c>.
/// </summary>
public static class AmbientScopeContext
{
    private static readonly AsyncLocal<ScopeContext?> Override = new();

    /// <summary>Pushes <paramref name="scope"/> until the returned handle is disposed (restore previous).</summary>
    public static IDisposable Push(ScopeContext scope)
    {
        var previous = Override.Value;
        Override.Value = scope;
        return new PopScope(previous);
    }

    /// <summary>Current ambient override, if any.</summary>
    public static ScopeContext? CurrentOverride => Override.Value;

    private sealed class PopScope(ScopeContext? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Override.Value = previous;
        }
    }
}
