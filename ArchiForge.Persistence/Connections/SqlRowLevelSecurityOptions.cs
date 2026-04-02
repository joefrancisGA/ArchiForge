using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Optional SQL Server row-level security rollout: apply <c>SESSION_CONTEXT</c> keys on each opened connection.
/// </summary>
/// <remarks>
/// DDL for the pilot <c>dbo.Runs</c> policy ships in DbUp <strong>030</strong> with <c>STATE = OFF</c>; operators enable the policy after verifying app behavior.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class SqlRowLevelSecurityOptions
{
    public const string SectionName = "SqlServer:RowLevelSecurity";

    /// <summary>When true, connections receive tenant/workspace/project (or bypass) session keys before queries run.</summary>
    public bool ApplySessionContext { get; set; }
}
