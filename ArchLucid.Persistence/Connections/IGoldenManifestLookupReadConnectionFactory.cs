namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Read connection for golden manifest lookup by scope + id (hot read path).
/// </summary>
public interface IGoldenManifestLookupReadConnectionFactory : IReadReplicaQueryConnectionFactory;
