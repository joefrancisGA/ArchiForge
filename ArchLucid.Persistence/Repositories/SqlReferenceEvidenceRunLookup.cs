using System.Diagnostics.CodeAnalysis;

using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     SQL lookup for committed runs by tenant (admin reference-evidence path). Uses the authority list connection
///     factory so results match dashboard-grade <see cref="SqlRunRepository" /> reads.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent; exercised via API integration tests with SQL.")]
public sealed class SqlReferenceEvidenceRunLookup(IAuthorityRunListConnectionFactory connectionFactory)
    : IReferenceEvidenceRunLookup
{
    private readonly IAuthorityRunListConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReferenceEvidenceRunCandidate>> ListRecentCommittedRunsAsync(
        Guid tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        int safeTake = Math.Clamp(take <= 0 ? 100 : take, 1, 500);

        const string sql = """
                           SELECT TOP (@Take)
                               r.RunId,
                               r.WorkspaceId,
                               r.ScopeProjectId,
                               r.ArchitectureRequestId AS RequestId
                           FROM dbo.Runs r WITH (NOLOCK)
                           WHERE r.TenantId = @TenantId
                             AND r.ArchivedUtc IS NULL
                             AND r.GoldenManifestId IS NOT NULL
                           ORDER BY r.CreatedUtc DESC;
                           """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ReferenceEvidenceRunCandidate> rows = await connection.QueryAsync<ReferenceEvidenceRunCandidate>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, Take = safeTake },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
