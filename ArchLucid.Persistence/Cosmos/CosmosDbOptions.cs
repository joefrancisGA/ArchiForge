using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Configuration for optional Azure Cosmos DB (NoSQL API) polyglot persistence.</summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO.")]
public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    /// <summary>Cosmos account connection string or emulator connection string.</summary>
    public string? ConnectionString
    {
        get;
        set;
    }

    /// <summary>Database id (logical database within the account).</summary>
    public string DatabaseName
    {
        get;
        set;
    } = "ArchLucid";

    /// <summary>
    ///     When true, <see cref="ArchLucid.KnowledgeGraph.Interfaces.IGraphSnapshotRepository" /> uses Cosmos instead of
    ///     SQL.
    /// </summary>
    public bool GraphSnapshotsEnabled
    {
        get;
        set;
    }

    /// <summary>
    ///     When true, <see cref="ArchLucid.Persistence.Data.Repositories.IAgentExecutionTraceRepository" /> uses Cosmos
    ///     instead of SQL.
    /// </summary>
    public bool AgentTracesEnabled
    {
        get;
        set;
    }

    /// <summary>When true, <see cref="ArchLucid.Persistence.Audit.IAuditRepository" /> uses Cosmos instead of SQL.</summary>
    public bool AuditEventsEnabled
    {
        get;
        set;
    }

    /// <summary>Default TTL in seconds for agent trace documents (Cosmos <c>ttl</c>); default 90 days.</summary>
    public int AgentTraceTtlSeconds
    {
        get;
        set;
    } = 90 * 24 * 60 * 60;

    /// <summary>Cosmos consistency level name: Session, BoundedStaleness, Strong, ConsistentPrefix, Eventual.</summary>
    public string DefaultConsistencyLevel
    {
        get;
        set;
    } = "Session";

    /// <summary>Processor instance name for change feed lease distribution (defaults to machine name).</summary>
    public string? ChangeFeedInstanceName
    {
        get;
        set;
    }

    public bool AnyCosmosFeatureEnabled =>
        GraphSnapshotsEnabled || AgentTracesEnabled || AuditEventsEnabled;
}
