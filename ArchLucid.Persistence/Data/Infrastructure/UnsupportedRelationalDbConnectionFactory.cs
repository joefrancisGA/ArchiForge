using System.Data;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     Placeholder <see cref="IDbConnectionFactory" /> for in-memory storage composition so types that
///     short-circuit on <see cref="ArchLucid.Core.Configuration.ArchLucidOptions.EffectiveIsInMemory" /> can still be
///     constructed.
///     When the host registers a real factory later (for example <see cref="SqlConnectionFactory" />), that registration
///     should win for resolution.
/// </summary>
public sealed class UnsupportedRelationalDbConnectionFactory : IDbConnectionFactory
{
    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        throw new InvalidOperationException(
            "Relational connections are not used when ArchLucid storage is InMemory unless a later IDbConnectionFactory registration replaces this placeholder.");
    }

    /// <inheritdoc />
    public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Relational connections are not used when ArchLucid storage is InMemory unless a later IDbConnectionFactory registration replaces this placeholder.");
    }
}
