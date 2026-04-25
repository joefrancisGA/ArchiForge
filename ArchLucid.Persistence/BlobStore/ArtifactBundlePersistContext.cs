namespace ArchLucid.Persistence.BlobStore;

/// <summary>Optional offload wiring for relational artifact inserts (null during SQL backfill).</summary>
internal readonly record struct ArtifactBundlePersistContext(
    IArtifactBlobStore BlobStore,
    ArtifactLargePayloadOptions Options);
