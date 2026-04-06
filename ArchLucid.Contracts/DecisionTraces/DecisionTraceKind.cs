namespace ArchiForge.Contracts.DecisionTraces;

/// <summary>JSON discriminator for polymorphic <see cref="DecisionTrace"/> (<see cref="RunEventTrace"/> vs <see cref="RuleAuditTrace"/>).</summary>
public enum DecisionTraceKind
{
    /// <summary>Merge/engine step log for string architecture runs (coordinator pipeline).</summary>
    RunEvent = 0,

    /// <summary>Authority rule-application audit from the decision engine.</summary>
    RuleAudit = 1
}
