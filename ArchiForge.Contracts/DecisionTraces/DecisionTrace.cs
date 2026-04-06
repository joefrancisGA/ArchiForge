namespace ArchiForge.Contracts.DecisionTraces;

/// <summary>
/// Unified trace model: coordinator <see cref="RunEventTracePayload"/> or authority <see cref="RuleAuditTracePayload"/>.
/// </summary>
public sealed class DecisionTrace
{
    public DecisionTraceKind Kind { get; set; }

    public RunEventTracePayload? RunEvent { get; set; }

    public RuleAuditTracePayload? RuleAudit { get; set; }

    public static DecisionTrace FromRunEvent(RunEventTracePayload body) =>
        new()
        {
            Kind = DecisionTraceKind.RunEvent,
            RunEvent = body ?? throw new ArgumentNullException(nameof(body))
        };

    public static DecisionTrace FromRuleAudit(RuleAuditTracePayload body) =>
        new()
        {
            Kind = DecisionTraceKind.RuleAudit,
            RuleAudit = body ?? throw new ArgumentNullException(nameof(body))
        };

    public RunEventTracePayload RequireRunEvent()
    {
        if (Kind != DecisionTraceKind.RunEvent || RunEvent is null)
            throw new InvalidOperationException("Expected a RunEvent decision trace.");

        return RunEvent;
    }

    public RuleAuditTracePayload RequireRuleAudit()
    {
        if (Kind != DecisionTraceKind.RuleAudit || RuleAudit is null)
            throw new InvalidOperationException("Expected a RuleAudit decision trace.");

        return RuleAudit;
    }
}
