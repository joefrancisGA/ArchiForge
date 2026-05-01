using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Scim;

public sealed class DapperScimTenantTokenRepository(ISqlConnectionFactory connectionFactory) : IScimTenantTokenRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScimTokenRotationCandidate>> ListActiveCreatedOnOrBeforeAsync(
        DateTimeOffset createdUtcUpperBoundInclusive,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT Id, TenantId, CreatedUtc
                           FROM dbo.ScimTenantTokens
                           WHERE RevokedUtc IS NULL AND CreatedUtc <= @Cutoff;
                           """;

        IEnumerable<ScimTokenRotationCandidate> rows = await connection.QueryAsync<ScimTokenRotationCandidate>(
            new CommandDefinition(
                sql,
                new { Cutoff = createdUtcUpperBoundInclusive },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<ScimTokenRow?> FindActiveByPublicLookupKeyAsync(
        string publicLookupKey,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT TOP (1) Id, TenantId, PublicLookupKey, SecretHash, CreatedUtc, RevokedUtc
                           FROM dbo.ScimTenantTokens
                           WHERE PublicLookupKey = @PublicLookupKey
                             AND RevokedUtc IS NULL;
                           """;

        TokenRow? row = await connection.QuerySingleOrDefaultAsync<TokenRow>(
            new CommandDefinition(sql, new { PublicLookupKey = publicLookupKey }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new ScimTokenRow
            {
                Id = row.Id,
                TenantId = row.TenantId,
                PublicLookupKey = row.PublicLookupKey,
                SecretHash = row.SecretHash,
                CreatedUtc = row.CreatedUtc,
                RevokedUtc = row.RevokedUtc
            };
    }

    /// <inheritdoc />
    public async Task<Guid> InsertAsync(
        Guid tenantId,
        string publicLookupKey,
        byte[] secretHash,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           INSERT INTO dbo.ScimTenantTokens (TenantId, PublicLookupKey, SecretHash)
                           OUTPUT INSERTED.Id
                           VALUES (@TenantId, @PublicLookupKey, @SecretHash);
                           """;

        Guid id = await connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, PublicLookupKey = publicLookupKey, SecretHash = secretHash },
                cancellationToken: cancellationToken));

        return id;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScimTokenSummaryRow>> ListForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT Id, CreatedUtc, RevokedUtc, PublicLookupKey
                           FROM dbo.ScimTenantTokens
                           WHERE TenantId = @TenantId
                           ORDER BY CreatedUtc DESC;
                           """;

        IEnumerable<ScimTokenSummaryRow> rows = await connection.QueryAsync<ScimTokenSummaryRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> TryRevokeByIdAsync(Guid tenantId, Guid tokenId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           UPDATE dbo.ScimTenantTokens
                           SET RevokedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id
                             AND TenantId = @TenantId
                             AND RevokedUtc IS NULL;
                           """;

        int rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = tokenId, TenantId = tenantId }, cancellationToken: cancellationToken));

        return rows == 1;
    }

    private sealed class TokenRow
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

        public string PublicLookupKey
        {
            get;
            init;
        } = string.Empty;

        public byte[] SecretHash
        {
            get;
            init;
        } = [];

        public DateTimeOffset CreatedUtc
        {
            get;
            init;
        }

        public DateTimeOffset? RevokedUtc
        {
            get;
            init;
        }
    }
}
