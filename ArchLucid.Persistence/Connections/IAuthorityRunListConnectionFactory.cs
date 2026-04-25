namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Connection factory used only for authority run <strong>list</strong> queries so operators can point heavy read
///     traffic at a replica.
/// </summary>
public interface IAuthorityRunListConnectionFactory : IReadReplicaQueryConnectionFactory;
