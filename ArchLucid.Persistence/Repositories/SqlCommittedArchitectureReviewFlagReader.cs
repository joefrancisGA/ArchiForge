using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Common;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Sql;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     SQL-backed <see cref="ICommittedArchitectureReviewFlagReader" /> using a single EXISTS against committed runs with
///     persisted golden manifests (same scope predicates as <see cref="SqlRunRepository.ListRecentInScopeAsync" />).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent reader; InMemory host uses RunRepositoryCommittedArchitectureReviewFlagReader.")]
public sealed class SqlCommittedArchitectureReviewFlagReader(IAuthorityRunListConnectionFactory authorityRunListConnectionFactory)
    : ICommittedArchitectureReviewFlagReader
{
    private readonly IAuthorityRunListConnectionFactory _authorityRunListConnectionFactory =
        authorityRunListConnectionFactory
        ?? throw new ArgumentNullException(nameof(authorityRunListConnectionFactory));

    private static readonly string CommittedLegacyStatus = ArchitectureRunStatus.Committed.ToString();

    /// <inheritdoc />
    public async Task<bool> TenantHasCommittedArchitectureReviewAsync(ScopeContext scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        await using SqlConnection connection =
            await _authorityRunListConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        int exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                HotPathRelationalQueryShapes.CommittedArchitectureReviewExistsNoLock,
                new
                {
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    CommittedStatus = CommittedLegacyStatus,
                },
                cancellationToken: cancellationToken));

        return exists != 0;
    }
}
