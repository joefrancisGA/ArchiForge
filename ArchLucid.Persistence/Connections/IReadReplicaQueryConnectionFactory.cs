using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Opens an optional read-scale-out SQL connection with the same RLS session context as primary repositories.
/// </summary>
public interface IReadReplicaQueryConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct);
}
