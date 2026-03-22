namespace ArchiForge.Decisioning.Alerts;

public class AlertRule
{
    public Guid RuleId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = default!;
    public string RuleType { get; set; } = default!;
    public string Severity { get; set; } = AlertSeverity.Warning;

    public decimal ThresholdValue { get; set; }
    public bool IsEnabled { get; set; } = true;

    public string TargetChannelType { get; set; } = "DigestOnly";
    public string MetadataJson { get; set; } = "{}";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
