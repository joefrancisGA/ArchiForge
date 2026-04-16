using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy;

public sealed class DapperTenantRepository(ISqlConnectionFactory connectionFactory) : ITenantRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<TenantRecord?> GetByIdAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             SELECT Id, Name, Slug, Tier, CreatedUtc, SuspendedUtc
                             FROM dbo.Tenants
                             WHERE Id = @Id;
                             """;

        TenantRow? row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            new CommandDefinition(sql, new { Id = tenantId }, cancellationToken: ct));

        return row is null ? null : row.ToRecord();
    }

    public async Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             SELECT Id, Name, Slug, Tier, CreatedUtc, SuspendedUtc
                             FROM dbo.Tenants
                             WHERE Slug = @Slug;
                             """;

        TenantRow? row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            new CommandDefinition(sql, new { Slug = slug.Trim().ToLowerInvariant() }, cancellationToken: ct));

        return row is null ? null : row.ToRecord();
    }

    public async Task<IReadOnlyList<TenantRecord>> ListAsync(CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             SELECT Id, Name, Slug, Tier, CreatedUtc, SuspendedUtc
                             FROM dbo.Tenants
                             ORDER BY CreatedUtc DESC;
                             """;

        IEnumerable<TenantRow> rows = await connection.QueryAsync<TenantRow>(new CommandDefinition(sql, cancellationToken: ct));

        return rows.Select(static r => r.ToRecord()).ToList();
    }

    public async Task InsertTenantAsync(
        Guid tenantId,
        string name,
        string slug,
        TenantTier tier,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             INSERT INTO dbo.Tenants (Id, Name, Slug, Tier)
                             VALUES (@Id, @Name, @Slug, @Tier);
                             """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = tenantId,
                    Name = name,
                    Slug = slug,
                    Tier = TenantTierSql.ToTierString(tier),
                },
                cancellationToken: ct));
    }

    public async Task InsertWorkspaceAsync(
        Guid workspaceId,
        Guid tenantId,
        string name,
        Guid defaultProjectId,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             INSERT INTO dbo.TenantWorkspaces (Id, TenantId, Name, DefaultProjectId)
                             VALUES (@Id, @TenantId, @Name, @DefaultProjectId);
                             """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = workspaceId,
                    TenantId = tenantId,
                    Name = name,
                    DefaultProjectId = defaultProjectId,
                },
                cancellationToken: ct));
    }

    public async Task SuspendTenantAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             UPDATE dbo.Tenants
                             SET SuspendedUtc = SYSUTCDATETIME()
                             WHERE Id = @Id;
                             """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = tenantId }, cancellationToken: ct));
    }

    public async Task<TenantWorkspaceLink?> GetFirstWorkspaceAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                             SELECT TOP (1) Id AS WorkspaceId, DefaultProjectId
                             FROM dbo.TenantWorkspaces
                             WHERE TenantId = @TenantId
                             ORDER BY CreatedUtc ASC;
                             """;

        WorkspaceRow? row = await connection.QuerySingleOrDefaultAsync<WorkspaceRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));

        if (row is null)
            return null;

        return new TenantWorkspaceLink
        {
            WorkspaceId = row.WorkspaceId,
            DefaultProjectId = row.DefaultProjectId,
        };
    }

    private sealed class WorkspaceRow
    {
        public Guid WorkspaceId { get; init; }

        public Guid DefaultProjectId { get; init; }
    }

    private sealed class TenantRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string Tier { get; init; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; init; }

        public DateTimeOffset? SuspendedUtc { get; init; }

        internal TenantRecord ToRecord() =>
            new()
            {
                Id = Id,
                Name = Name,
                Slug = Slug,
                Tier = TenantTierSql.ParseTier(Tier),
                CreatedUtc = CreatedUtc,
                SuspendedUtc = SuspendedUtc,
            };
    }
}
