using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Manifest;
using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Dapper-backed coordinator manifest port — <b>retired for SQL</b> after ADR 0030 PR A4 (migration 111 drops
/// <c>dbo.GoldenManifestVersions</c>). <see cref="CreateAsync"/> throws; <see cref="GetByVersionAsync"/> returns
/// <see langword="null"/>. Use <see cref="InMemoryCoordinatorGoldenManifestRepository"/> for in-memory coordinator flows.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL coordinator manifest store removed; throws on write path.")]
public sealed class GoldenManifestRepository(IDbConnectionFactory connectionFactory) : ICoordinatorGoldenManifestRepository
{
    private readonly IDbConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public Task CreateAsync(
        GoldenManifest manifest,
        CancellationToken cancellationToken = default,
        System.Data.IDbConnection? connection = null,
        System.Data.IDbTransaction? transaction = null)
    {
        if (manifest is null)
            throw new ArgumentNullException(nameof(manifest));

        _ = _connectionFactory;
        _ = connection;
        _ = transaction;

        throw new InvalidOperationException(
            "Legacy dbo.GoldenManifestVersions was removed (ADR 0030 PR A4, migration 111). " +
            "The coordinator SQL manifest row store is retired; use the Authority commit path (Coordinator:LegacyRunCommitPath=false) " +
            "and dbo.GoldenManifests. For in-memory coordinator semantics use InMemoryCoordinatorGoldenManifestRepository.");
    }

    /// <inheritdoc />
    public Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestVersion))
            throw new ArgumentException("Manifest version is required.", nameof(manifestVersion));

        _ = _connectionFactory;

        return Task.FromResult<GoldenManifest?>(null);
    }
}
