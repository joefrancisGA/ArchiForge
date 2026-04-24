using ArchLucid.Persistence.Connections;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>
///     Minimal <see cref="IOptionsMonitor{TOptions}" /> for integration tests (mutable
///     <see cref="SqlServerOptions" /> instance).
/// </summary>
public sealed class FixedSqlServerOptionsMonitor(SqlServerOptions instance) : IOptionsMonitor<SqlServerOptions>
{
    public SqlServerOptions CurrentValue
    {
        get;
    } = instance ?? throw new ArgumentNullException(nameof(instance));

    public SqlServerOptions Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<SqlServerOptions, string> listener)
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
