using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.Connections;

/// <summary>
/// SQL Server connectivity and security options under configuration section <c>SqlServer</c>.
/// </summary>
/// <remarks>
/// Consolidates <c>RowLevelSecurity</c> and <c>ReadReplica</c> children previously bound as separate options types.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    /// <summary>Row-level security rollout: apply <c>SESSION_CONTEXT</c> keys on each opened connection.</summary>
    public SqlRowLevelSecuritySettings RowLevelSecurity { get; set; } = new();

    /// <summary>Optional read replica for specific hot read paths.</summary>
    public SqlReadReplicaSettings ReadReplica { get; set; } = new();
}

/// <summary>Binding for <c>SqlServer:RowLevelSecurity</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class SqlRowLevelSecuritySettings
{
    /// <summary>When true, connections receive tenant/workspace/project (or bypass) session keys before queries run.</summary>
    public bool ApplySessionContext { get; set; }
}

/// <summary>Binding for <c>SqlServer:ReadReplica</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class SqlReadReplicaSettings
{
    /// <summary>
    /// Azure SQL failover group <strong>read-only listener</strong> (secondary replica). Used for governance-resolution and
    /// golden-manifest lookup reads when set; also used for authority run lists when <see cref="AuthorityRunListReadsConnectionString"/> is unset.
    /// </summary>
    public string? FailoverGroupReadOnlyListenerConnectionString { get; set; }

    /// <summary>When set, authority run list reads prefer this connection over <see cref="FailoverGroupReadOnlyListenerConnectionString"/>.</summary>
    public string? AuthorityRunListReadsConnectionString { get; set; }
}
