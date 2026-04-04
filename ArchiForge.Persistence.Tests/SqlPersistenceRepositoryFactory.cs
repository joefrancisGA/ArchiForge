using ArchiForge.Persistence.BlobStore;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchiForge.Persistence.Tests;

/// <summary>Constructs SQL repositories with large-payload offload disabled (default for integration tests).</summary>
internal static class SqlPersistenceRepositoryFactory
{
    private static readonly IOptionsMonitor<ArtifactLargePayloadOptions> DisabledLargePayloadOptions =
        CreateDisabledLargePayloadOptions();

    internal static SqlGoldenManifestRepository CreateGoldenManifestRepository(ISqlConnectionFactory factory)
    {
        SqlPrimaryMirroredReadReplicaConnectionFactory readMirror = new(factory);
        return new SqlGoldenManifestRepository(factory, readMirror, new NullArtifactBlobStore(), DisabledLargePayloadOptions);
    }

    internal static SqlArtifactBundleRepository CreateArtifactBundleRepository(ISqlConnectionFactory factory) =>
        new(factory, new NullArtifactBlobStore(), DisabledLargePayloadOptions);

    private static IOptionsMonitor<ArtifactLargePayloadOptions> CreateDisabledLargePayloadOptions()
    {
        Mock<IOptionsMonitor<ArtifactLargePayloadOptions>> mock = new();
        mock.Setup(static o => o.CurrentValue).Returns(
            new ArtifactLargePayloadOptions
            {
                Enabled = false,
                BlobProvider = "None",
            });
        return mock.Object;
    }
}
