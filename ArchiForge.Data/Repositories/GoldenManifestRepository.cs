using System.Data;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="GoldenManifest"/> versions, serialising manifest state as JSON.
/// </summary>
public sealed class GoldenManifestRepository(IDbConnectionFactory connectionFactory) : IGoldenManifestRepository
{
    public async Task CreateAsync(GoldenManifest manifest, CancellationToken cancellationToken = default)
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

        using IDbConnection connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
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
            cancellationToken: cancellationToken));
    }

    public async Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ManifestJson
            FROM GoldenManifestVersions
            WHERE ManifestVersion = @ManifestVersion;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

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
