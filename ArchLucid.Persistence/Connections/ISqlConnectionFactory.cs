using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Connections;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct);
}
