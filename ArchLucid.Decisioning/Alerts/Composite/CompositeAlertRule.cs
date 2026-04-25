namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     Multi-condition alert definition: combines metric predicates with AND/OR and defines deduplication windows.
/// </summary>
/// <remarks>
///     Persisted as header + <see cref="Conditions" /> child rows. Evaluated via
///     <see cref="ICompositeAlertRuleEvaluator" /> after <see cref="IAlertMetricSnapshotBuilder" />.
/// </remarks>
public class CompositeAlertRule
{
    /// <summary>Primary key.</summary>
    public Guid CompositeRuleId
    {
        get;
        set;
    } = Guid.NewGuid();

    /// <summary>Tenant scope.</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Workspace scope.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Project scope.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>Operator display name.</summary>
    public string Name
    {
        get;
        set;
    } = null!;

    /// <summary>Severity assigned to created <see cref="AlertRecord" /> rows.</summary>
    public string Severity
    {
        get;
        set;
    } = AlertSeverity.Warning;

    /// <summary>AND/OR across <see cref="Conditions" /> (<see cref="CompositeOperator" />).</summary>
    public string Operator
    {
        get;
        set;
    } = CompositeOperator.And;

    /// <summary>When false, excluded from enabled rule queries.</summary>
    public bool IsEnabled
    {
        get;
        set;
    } = true;

    /// <summary>Minimum minutes between repeat fires for the same dedupe key (see <see cref="IAlertSuppressionPolicy" />).</summary>
    public int SuppressionWindowMinutes
    {
        get;
        set;
    } = 1440;

    /// <summary>Short cooldown after a prior alert on the same key.</summary>
    public int CooldownMinutes
    {
        get;
        set;
    } = 60;

    /// <summary>Reserved for reopen semantics; not all policies use this yet.</summary>
    public decimal ReopenDeltaThreshold
    {
        get;
        set;
    }

    /// <summary>How dedupe keys incorporate run ids (<see cref="CompositeDedupeScope" />).</summary>
    public string DedupeScope
    {
        get;
        set;
    } = CompositeDedupeScope.RuleAndRun;

    /// <summary>Routing hint (often <c>AlertRouting</c> for composite alerts).</summary>
    public string TargetChannelType
    {
        get;
        set;
    } = "AlertRouting";

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;

    /// <summary>Child conditions loaded with the rule from persistence.</summary>
    public List<AlertRuleCondition> Conditions
    {
        get;
        set;
    } = [];
}
