using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Dapper-backed persistence for <see cref="IEvidenceBundleRepository" />; writes and reads evidence bundle records
///     from the <c>EvidenceBundles</c> table.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class EvidenceBundleRepository(IDbConnectionFactory connectionFactory) : IEvidenceBundleRepository
{
    public async Task CreateAsync(
        EvidenceBundle evidenceBundle,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        const string sql = """
                           INSERT INTO EvidenceBundles
                           (
                               EvidenceBundleId,
                               RequestDescription,
                               EvidenceJson,
                               CreatedUtc
                           )
                           VALUES
                           (
                               @EvidenceBundleId,
                               @RequestDescription,
                               @EvidenceJson,
                               @CreatedUtc
                           );
                           """;

        string json = JsonSerializer.Serialize(evidenceBundle, ContractJson.Default);

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    evidenceBundle.EvidenceBundleId,
                    evidenceBundle.RequestDescription,
                    EvidenceJson = json,
                    CreatedUtc = DateTime.UtcNow
                },
                transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    public async Task<EvidenceBundle?> GetByIdAsync(string evidenceBundleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT EvidenceJson
                           FROM EvidenceBundles
                           WHERE EvidenceBundleId = @EvidenceBundleId;
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new { EvidenceBundleId = evidenceBundleId },
            cancellationToken: cancellationToken));

        if (json is null)
            return null;

        EvidenceBundle? bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<EvidenceBundle>(json, ContractJson.Default);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Evidence bundle JSON for '{evidenceBundleId}' could not be deserialized. " +
                "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }

        return bundle
               ?? throw new InvalidOperationException(
                   $"Evidence bundle JSON for '{evidenceBundleId}' deserialized to null. " +
                   "The stored JSON may be empty or corrupt.");
    }
}
