using ArchiForge.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Test double: routes list reads to the same primary factory as <see cref="Repositories.SqlRunRepository"/> writes,
/// avoiding read-replica and RLS wiring in persistence contract tests.
/// </summary>
public sealed class TestAuthorityRunListConnectionFactory(ISqlConnectionFactory primary) : IAuthorityRunListConnectionFactory
{
    private readonly ISqlConnectionFactory _primary =
        primary ?? throw new ArgumentNullException(nameof(primary));

    /// <inheritdoc />
    public Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct) =>
        _primary.CreateOpenConnectionAsync(ct);
}
