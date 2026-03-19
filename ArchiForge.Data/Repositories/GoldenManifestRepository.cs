using System.Text.Json;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class GoldenManifestRepository(IDbConnectionFactory connectionFactory) : IGoldenManifestRepository
{
    public async Task CreateAsync(GoldenManifest manifest, CancellationToken cancellationToken = default)
    {
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

        var json = JsonSerializer.Serialize(manifest, ContractJson.Default);

        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                ManifestVersion = manifest.Metadata.ManifestVersion,
                manifest.RunId,
                manifest.SystemName,
                ManifestJson = json,
                ParentManifestVersion = manifest.Metadata.ParentManifestVersion,
                CreatedUtc = manifest.Metadata.CreatedUtc
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

        using var connection = connectionFactory.CreateConnection();

        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new { ManifestVersion = manifestVersion },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<GoldenManifest>(json, ContractJson.Default);
    }
}