using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Applies <c>sp_set_session_context</c> for row-level security on an open <see cref="SqlConnection" />.
/// </summary>
public interface IRlsSessionContextApplicator
{
    Task ApplyAsync(SqlConnection connection, CancellationToken ct);
}
