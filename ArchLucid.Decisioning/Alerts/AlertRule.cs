namespace ArchLucid.Decisioning.Alerts;

/// <summary>
///     Configuration for a single metric-based alert (non-composite), stored per scope in <c>dbo.AlertRules</c>.
/// </summary>
/// <remarks>
///     <see cref="RuleType" /> values correspond to <see cref="AlertRuleType" />; evaluated by
///     <see cref="AlertEvaluator" />.
/// </remarks>
public class AlertRule
{
    /// <summary>Primary key.</summary>
    public Guid RuleId
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

    /// <summary>Discriminator for evaluator switch (<see cref="AlertRuleType" />).</summary>
    public string RuleType
    {
        get;
        set;
    } = null!;

    /// <summary>Default severity when the rule fires.</summary>
    public string Severity
    {
        get;
        set;
    } = AlertSeverity.Warning;

    /// <summary>Compared to live metrics per <see cref="RuleType" /> (count, percent, days, etc.).</summary>
    public decimal ThresholdValue
    {
        get;
        set;
    }

    /// <summary>Disabled rules are omitted from <see cref="IAlertRuleRepository.ListEnabledByScopeAsync" />.</summary>
    public bool IsEnabled
    {
        get;
        set;
    } = true;

    /// <summary>Hint for routing (e.g. <c>DigestOnly</c> vs alert routing); interpretation is host-specific.</summary>
    public string TargetChannelType
    {
        get;
        set;
    } = "DigestOnly";

    /// <summary>Opaque JSON for future rule options.</summary>
    public string MetadataJson
    {
        get;
        set;
    } = "{}";

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;
}
