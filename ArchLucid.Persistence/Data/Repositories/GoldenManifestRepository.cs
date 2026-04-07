using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="GoldenManifest"/> versions, serialising manifest state as JSON.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class GoldenManifestRepository(IDbConnectionFactory connectionFactory) : ICoordinatorGoldenManifestRepository
{
    public async Task CreateAsync(
        GoldenManifest manifest,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        const string sql = """
            INSERT INTO GoldenManifestVersions
            (
                ManifestVersion,
                RunId,
                SystemName,
                ManifestJson,
                ParentManifestVersion,
                CreatedUtc
            )
            VALUES
            (
                @ManifestVersion,
                @RunId,
                @SystemName,
                @ManifestJson,
                @ParentManifestVersion,
                @CreatedUtc
            );
            """;

        string json = JsonSerializer.Serialize(manifest, ContractJson.Default);

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    manifest.Metadata.ManifestVersion,
                    manifest.RunId,
                    manifest.SystemName,
                    ManifestJson = json,
                    manifest.Metadata.ParentManifestVersion,
                    manifest.Metadata.CreatedUtc
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    public async Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ManifestJson
            FROM GoldenManifestVersions
            WHERE ManifestVersion = @ManifestVersion;
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                ManifestVersion = manifestVersion
            },
            cancellationToken: cancellationToken));

        if (json is null)
            return null;

        GoldenManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<GoldenManifest>(json, ContractJson.Default);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Manifest JSON for version '{manifestVersion}' could not be deserialized. " +
                "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }

        return manifest
            ?? throw new InvalidOperationException(
                $"Manifest JSON for version '{manifestVersion}' deserialized to null. " +
                "The stored JSON may be empty or corrupt.");
    }
}
