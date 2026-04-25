using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Scim;

public sealed class DapperScimGroupRepository(ISqlConnectionFactory connectionFactory) : IScimGroupRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ScimGroupRecord> items, int totalCount)> ListAsync(
        Guid tenantId,
        int startIndex1Based,
        int count,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string countSql = """
                                SELECT COUNT(1)
                                FROM dbo.ScimGroups g
                                WHERE g.TenantId = @TenantId;
                                """;

        int total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        if (count <= 0)
            return ([], total);

        int offset = Math.Max(0, startIndex1Based - 1);

        const string listSql = """
                               SELECT g.Id, g.TenantId, g.ExternalId, g.DisplayName, g.CreatedUtc, g.UpdatedUtc
                               FROM dbo.ScimGroups g
                               WHERE g.TenantId = @TenantId
                               ORDER BY g.CreatedUtc
                               OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                               """;

        IEnumerable<GroupRow> rows = await connection.QueryAsync<GroupRow>(
            new CommandDefinition(
                listSql,
                new { TenantId = tenantId, Offset = offset, PageSize = count },
                cancellationToken: cancellationToken));

        return (rows.Select(static r => r.ToRecord()).ToList(), total);
    }

    /// <inheritdoc />
    public async Task<ScimGroupRecord?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT g.Id, g.TenantId, g.ExternalId, g.DisplayName, g.CreatedUtc, g.UpdatedUtc
                           FROM dbo.ScimGroups g
                           WHERE g.TenantId = @TenantId AND g.Id = @Id;
                           """;

        GroupRow? row = await connection.QuerySingleOrDefaultAsync<GroupRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, Id = id }, cancellationToken: cancellationToken));

        return row?.ToRecord();
    }

    /// <inheritdoc />
    public async Task<ScimGroupRecord> InsertAsync(
        Guid tenantId,
        string externalId,
        string displayName,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           INSERT INTO dbo.ScimGroups (TenantId, ExternalId, DisplayName)
                           OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.ExternalId, INSERTED.DisplayName, INSERTED.CreatedUtc, INSERTED.UpdatedUtc
                           VALUES (@TenantId, @ExternalId, @DisplayName);
                           """;

        GroupRow row = await connection.QuerySingleAsync<GroupRow>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, ExternalId = externalId, DisplayName = displayName },
                cancellationToken: cancellationToken));

        return row.ToRecord();
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(
        Guid tenantId,
        Guid id,
        string externalId,
        string displayName,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           UPDATE dbo.ScimGroups
                           SET ExternalId = @ExternalId,
                               DisplayName = @DisplayName,
                               UpdatedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id AND TenantId = @TenantId;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, Id = id, ExternalId = externalId, DisplayName = displayName },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task SetMembersAsync(
        Guid tenantId,
        Guid groupId,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using SqlTransaction tran = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        const string del = """
                           DELETE FROM dbo.ScimGroupMembers
                           WHERE GroupId = @GroupId AND TenantId = @TenantId;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(del, new { GroupId = groupId, TenantId = tenantId }, tran, cancellationToken: cancellationToken));

        const string ins = """
                           INSERT INTO dbo.ScimGroupMembers (TenantId, GroupId, UserId)
                           VALUES (@TenantId, @GroupId, @UserId);
                           """;

        foreach (Guid userId in userIds)

            await connection.ExecuteAsync(
                new CommandDefinition(ins, new { TenantId = tenantId, GroupId = groupId, UserId = userId }, tran, cancellationToken: cancellationToken));


        await tran.CommitAsync(cancellationToken);
    }

    private sealed class GroupRow
    {
        public Guid Id
        {
            get;
            init;
        }

        public Guid TenantId
        {
            get;
            init;
        }

        public string ExternalId
        {
            get;
            init;
        } = string.Empty;

        public string DisplayName
        {
            get;
            init;
        } = string.Empty;

        public DateTimeOffset CreatedUtc
        {
            get;
            init;
        }

        public DateTimeOffset UpdatedUtc
        {
            get;
            init;
        }

        internal ScimGroupRecord ToRecord()
        {
            return new ScimGroupRecord
            {
                Id = Id,
                TenantId = TenantId,
                ExternalId = ExternalId,
                DisplayName = DisplayName,
                CreatedUtc = CreatedUtc,
                UpdatedUtc = UpdatedUtc
            };
        }
    }
}
