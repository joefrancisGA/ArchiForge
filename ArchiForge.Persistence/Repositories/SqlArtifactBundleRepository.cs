using System.Data;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

public sealed class SqlArtifactBundleRepository : IArtifactBundleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlArtifactBundleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        ArtifactBundle bundle,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.ArtifactBundles
            (
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson
            )
            VALUES
            (
                @BundleId, @RunId, @ManifestId, @CreatedUtc, @ArtifactsJson, @TraceJson
            );
            """;

        var args = new
        {
            bundle.BundleId,
            bundle.RunId,
            bundle.ManifestId,
            bundle.CreatedUtc,
            ArtifactsJson = JsonEntitySerializer.Serialize(bundle.Artifacts),
            TraceJson = JsonEntitySerializer.Serialize(bundle.Trace)
        };

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<ArtifactBundle?> GetByManifestIdAsync(Guid manifestId, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson
            FROM dbo.ArtifactBundles
            WHERE ManifestId = @ManifestId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<ArtifactBundleRow>(
            new CommandDefinition(sql, new { ManifestId = manifestId }, cancellationToken: ct));

        if (row is null)
            return null;

        return new ArtifactBundle
        {
            BundleId = row.BundleId,
            RunId = row.RunId,
            ManifestId = row.ManifestId,
            CreatedUtc = row.CreatedUtc,
            Artifacts = JsonEntitySerializer.Deserialize<List<SynthesizedArtifact>>(row.ArtifactsJson),
            Trace = JsonEntitySerializer.Deserialize<SynthesisTrace>(row.TraceJson)
        };
    }

    private sealed class ArtifactBundleRow
    {
        public Guid BundleId { get; set; }
        public Guid RunId { get; set; }
        public Guid ManifestId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string ArtifactsJson { get; set; } = default!;
        public string TraceJson { get; set; } = default!;
    }
}
