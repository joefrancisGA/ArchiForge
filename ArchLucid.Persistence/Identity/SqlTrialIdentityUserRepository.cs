using ArchLucid.Core.Identity;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Identity;

public sealed class SqlTrialIdentityUserRepository(ISqlConnectionFactory connectionFactory)
    : ITrialIdentityUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<TrialIdentityUserRecord?> GetByNormalizedEmailAsync(string normalizedEmail,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT TOP (1)
                               Id,
                               NormalizedEmail,
                               Email,
                               PasswordHash,
                               SecurityStamp,
                               ConcurrencyStamp,
                               EmailConfirmed,
                               EmailVerifiedUtc,
                               LockoutEnd,
                               LockoutEnabled,
                               AccessFailedCount,
                               EmailConfirmationTokenHash,
                               EmailConfirmationExpiresUtc,
                               LinkedEntraOid,
                               LinkedUtc
                           FROM dbo.IdentityUsers
                           WHERE NormalizedEmail = @NormalizedEmail;
                           """;

        return await connection.QuerySingleOrDefaultAsync<TrialIdentityUserRecord>(
            new CommandDefinition(sql, new { NormalizedEmail = normalizedEmail },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Guid> CreatePendingUserAsync(
        string normalizedEmail,
        string email,
        string passwordHash,
        string securityStamp,
        string concurrencyStamp,
        string emailConfirmationTokenHash,
        DateTimeOffset emailConfirmationExpiresUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        using (SqlRowLevelSecurityBypassAmbient.Enter())
        {
            await using SqlConnection connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            const string sql = """
                               INSERT INTO dbo.IdentityUsers
                               (
                                   NormalizedEmail,
                                   Email,
                                   PasswordHash,
                                   SecurityStamp,
                                   ConcurrencyStamp,
                                   EmailConfirmed,
                                   EmailVerifiedUtc,
                                   EmailConfirmationTokenHash,
                                   EmailConfirmationExpiresUtc
                               )
                               OUTPUT INSERTED.Id
                               VALUES
                               (
                                   @NormalizedEmail,
                                   @Email,
                                   @PasswordHash,
                                   @SecurityStamp,
                                   @ConcurrencyStamp,
                                   0,
                                   NULL,
                                   @EmailConfirmationTokenHash,
                                   @EmailConfirmationExpiresUtc
                               );
                               """;

            Guid id = await connection.ExecuteScalarAsync<Guid>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        NormalizedEmail = normalizedEmail,
                        Email = email,
                        PasswordHash = passwordHash,
                        SecurityStamp = securityStamp,
                        ConcurrencyStamp = concurrencyStamp,
                        EmailConfirmationTokenHash = emailConfirmationTokenHash,
                        EmailConfirmationExpiresUtc = emailConfirmationExpiresUtc
                    },
                    cancellationToken: cancellationToken));

            return id;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryConfirmEmailAsync(
        string normalizedEmail,
        string emailConfirmationTokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(emailConfirmationTokenHash);

        using (SqlRowLevelSecurityBypassAmbient.Enter())
        {
            await using SqlConnection connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            const string sql = """
                               UPDATE dbo.IdentityUsers
                               SET EmailConfirmed = 1,
                                   EmailVerifiedUtc = @NowUtc,
                                   EmailConfirmationTokenHash = NULL,
                                   EmailConfirmationExpiresUtc = NULL,
                                   ConcurrencyStamp = NEWID()
                               WHERE NormalizedEmail = @NormalizedEmail
                                 AND EmailConfirmationTokenHash = @TokenHash
                                 AND EmailConfirmationExpiresUtc > @NowUtc;
                               """;

            int rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { NormalizedEmail = normalizedEmail, TokenHash = emailConfirmationTokenHash, NowUtc = nowUtc },
                    cancellationToken: cancellationToken));

            return rows == 1;
        }
    }

    /// <inheritdoc />
    public async Task RecordAccessFailedAsync(
        string normalizedEmail,
        int newCount,
        DateTimeOffset? lockoutEnd,
        CancellationToken cancellationToken)
    {
        using (SqlRowLevelSecurityBypassAmbient.Enter())
        {
            await using SqlConnection connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            const string sql = """
                               UPDATE dbo.IdentityUsers
                               SET AccessFailedCount = @NewCount,
                                   LockoutEnd = @LockoutEnd
                               WHERE NormalizedEmail = @NormalizedEmail;
                               """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { NormalizedEmail = normalizedEmail, NewCount = newCount, LockoutEnd = lockoutEnd },
                    cancellationToken: cancellationToken));
        }
    }

    /// <inheritdoc />
    public async Task ResetAccessFailedAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using (SqlRowLevelSecurityBypassAmbient.Enter())
        {
            await using SqlConnection connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            const string sql = """
                               UPDATE dbo.IdentityUsers
                               SET AccessFailedCount = 0,
                                   LockoutEnd = NULL
                               WHERE NormalizedEmail = @NormalizedEmail;
                               """;

            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { NormalizedEmail = normalizedEmail },
                    cancellationToken: cancellationToken));
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryLinkLocalIdentityToEntraAsync(
        string normalizedEmail,
        string entraOid,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(entraOid);

        string oid = entraOid.Trim();

        if (oid.Length > 128)
            throw new ArgumentException("Entra OID must be at most 128 characters.", nameof(entraOid));

        TrialIdentityUserRecord? row = await GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (row is null)
            return false;

        if (row.LinkedEntraOid is string linked && linked != oid)
            return false;

        if (string.Equals(row.LinkedEntraOid, oid, StringComparison.Ordinal))
            return true;

        using (SqlRowLevelSecurityBypassAmbient.Enter())
        {
            await using SqlConnection connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            DateTimeOffset linkedUtc = DateTimeOffset.UtcNow;

            const string sql = """
                               UPDATE dbo.IdentityUsers
                               SET LinkedEntraOid = @Oid,
                                   LinkedUtc = @LinkedUtc,
                                   ConcurrencyStamp = NEWID()
                               WHERE NormalizedEmail = @NormalizedEmail
                                 AND (LinkedEntraOid IS NULL OR LinkedEntraOid = @Oid);
                               """;

            int rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { NormalizedEmail = normalizedEmail, Oid = oid, LinkedUtc = linkedUtc },
                    cancellationToken: cancellationToken));

            return rows == 1;
        }
    }
}
