using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Alerts.Composite;

public class CompositeAlertRule
{
    public Guid CompositeRuleId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = null!;
    public string Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>AND/OR across conditions (<see cref="CompositeOperator"/>).</summary>
    public string Operator { get; set; } = CompositeOperator.And;

    public bool IsEnabled { get; set; } = true;

    public int SuppressionWindowMinutes { get; set; } = 1440;
    public int CooldownMinutes { get; set; } = 60;

    public decimal ReopenDeltaThreshold { get; set; }
    public string DedupeScope { get; set; } = CompositeDedupeScope.RuleAndRun;

    public string TargetChannelType { get; set; } = "AlertRouting";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<AlertRuleCondition> Conditions { get; set; } = [];
}
