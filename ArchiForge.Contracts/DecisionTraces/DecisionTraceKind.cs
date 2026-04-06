namespace ArchiForge.Contracts.DecisionTraces;

/// <summary>Discriminator for <see cref="DecisionTrace"/> payloads (coordinator vs authority).</summary>
public enum DecisionTraceKind
{
    /// <summary>Merge/engine step log for string architecture runs (coordinator pipeline).</summary>
    RunEvent = 0,

    /// <summary>Authority rule-application audit from the decision engine.</summary>
    RuleAudit = 1
}
