using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Filtering;
using ArchLucid.Core.Scim.Models;

using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Utilities;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Scim;

public sealed class DapperScimUserRepository(ISqlConnectionFactory connectionFactory) : IScimUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ScimUserRecord> items, int totalCount)> ListAsync(
        Guid tenantId,
        ScimFilterNode? filter,
        int startIndex1Based,
        int count,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = new();
        parameters.Add("TenantId", tenantId);
        int p = 0;
        string whereExtra = SqlScimUserFilterTranslator.BuildWhere(filter, parameters, ref p);

        string countSql = $"""
                             SELECT COUNT(1)
                             FROM dbo.ScimUsers u
                             WHERE u.TenantId = @TenantId AND ({whereExtra});
                             """;

        int total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (count <= 0)
            return ([], total);

        int offset = Math.Max(0, startIndex1Based - 1);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", count);

        string listSql = $"""
                            SELECT u.Id, u.TenantId, u.ExternalId, u.UserName, u.DisplayName, u.Active, u.ResolvedRole,
                                   u.CreatedUtc, u.UpdatedUtc
                            FROM dbo.ScimUsers u
                            WHERE u.TenantId = @TenantId AND ({whereExtra})
                            ORDER BY u.CreatedUtc
                            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                            """;

        IEnumerable<UserRow> rows = await connection.QueryAsync<UserRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return (rows.Select(static r => r.ToRecord()).ToList(), total);
    }

    /// <inheritdoc />
    public async Task<ScimUserRecord?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT u.Id, u.TenantId, u.ExternalId, u.UserName, u.DisplayName, u.Active, u.ResolvedRole,
                                  u.CreatedUtc, u.UpdatedUtc
                           FROM dbo.ScimUsers u
                           WHERE u.TenantId = @TenantId AND u.Id = @Id;
                           """;

        UserRow? row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, Id = id }, cancellationToken: cancellationToken));

        return row?.ToRecord();
    }

    /// <inheritdoc />
    public async Task<ScimUserRecord?> GetByExternalIdAsync(
        Guid tenantId,
        string externalId,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT u.Id, u.TenantId, u.ExternalId, u.UserName, u.DisplayName, u.Active, u.ResolvedRole,
                                  u.CreatedUtc, u.UpdatedUtc
                           FROM dbo.ScimUsers u
                           WHERE u.TenantId = @TenantId AND u.ExternalId = @ExternalId;
                           """;

        UserRow? row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, ExternalId = externalId }, cancellationToken: cancellationToken));

        return row?.ToRecord();
    }

    /// <inheritdoc />
    public async Task<ScimUserRecord> InsertAsync(
        Guid tenantId,
        string externalId,
        string userName,
        string? displayName,
        bool active,
        string? resolvedRole,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           INSERT INTO dbo.ScimUsers (TenantId, ExternalId, UserName, DisplayName, Active, ResolvedRole)
                           OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.ExternalId, INSERTED.UserName, INSERTED.DisplayName,
                                  INSERTED.Active, INSERTED.ResolvedRole, INSERTED.CreatedUtc, INSERTED.UpdatedUtc
                           VALUES (@TenantId, @ExternalId, @UserName, @DisplayName, @Active, @ResolvedRole);
                           """;

        UserRow? outRow = await connection.QueryFirstOrDefaultAsync<UserRow>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    ExternalId = externalId,
                    UserName = userName,
                    DisplayName = displayName,
                    Active = active,
                    ResolvedRole = resolvedRole
                },
                cancellationToken: cancellationToken));

        return DapperRowExpect
            .Required(outRow, "SCIM user insert must return OUTPUT row from dbo.ScimUsers.")
            .ToRecord();
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(
        Guid tenantId,
        Guid id,
        string externalId,
        string userName,
        string? displayName,
        bool active,
        string? resolvedRole,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           UPDATE dbo.ScimUsers
                           SET ExternalId = @ExternalId,
                               UserName = @UserName,
                               DisplayName = @DisplayName,
                               Active = @Active,
                               ResolvedRole = @ResolvedRole,
                               UpdatedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id AND TenantId = @TenantId;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    Id = id,
                    ExternalId = externalId,
                    UserName = userName,
                    DisplayName = displayName,
                    Active = active,
                    ResolvedRole = resolvedRole
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task PatchAsync(
        Guid tenantId,
        Guid id,
        string? externalId,
        string? userName,
        string? displayName,
        bool? active,
        string? resolvedRole,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           UPDATE dbo.ScimUsers
                           SET ExternalId = COALESCE(@ExternalId, ExternalId),
                               UserName = COALESCE(@UserName, UserName),
                               DisplayName = CASE WHEN @DisplayNameProvided = 1 THEN @DisplayName ELSE DisplayName END,
                               Active = COALESCE(@Active, Active),
                               ResolvedRole = COALESCE(@ResolvedRole, ResolvedRole),
                               UpdatedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id AND TenantId = @TenantId;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    Id = id,
                    ExternalId = externalId,
                    UserName = userName,
                    DisplayName = displayName,
                    DisplayNameProvided = displayName is null ? 0 : 1,
                    Active = active,
                    ResolvedRole = resolvedRole
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           UPDATE dbo.ScimUsers
                           SET Active = 0,
                               UpdatedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id AND TenantId = @TenantId;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { TenantId = tenantId, Id = id }, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string DisplayName, string ExternalId)>> ListGroupKeysForUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT g.DisplayName, g.ExternalId
                           FROM dbo.ScimGroupMembers m
                           INNER JOIN dbo.ScimGroups g ON g.Id = m.GroupId
                           WHERE m.TenantId = @TenantId AND m.UserId = @UserId;
                           """;

        IEnumerable<GroupKeyRow> rows = await connection.QueryAsync<GroupKeyRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, cancellationToken: cancellationToken));

        return rows.Select(static r => (r.DisplayName, r.ExternalId)).ToList();
    }

    private sealed class GroupKeyRow
    {
        public string DisplayName
        {
            get;
            init;
        } = string.Empty;

        public string ExternalId
        {
            get;
            init;
        } = string.Empty;
    }

    private sealed class UserRow
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

        public string UserName
        {
            get;
            init;
        } = string.Empty;

        public string? DisplayName
        {
            get;
            init;
        }

        public bool Active
        {
            get;
            init;
        }

        public string? ResolvedRole
        {
            get;
            init;
        }

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

        internal ScimUserRecord ToRecord()
        {
            return new ScimUserRecord
            {
                Id = Id,
                TenantId = TenantId,
                ExternalId = ExternalId,
                UserName = UserName,
                DisplayName = DisplayName,
                Active = Active,
                ResolvedRole = ResolvedRole,
                CreatedUtc = CreatedUtc,
                UpdatedUtc = UpdatedUtc
            };
        }
    }
}
