using System.Text.Json.Serialization;

namespace ArchiForge.Contracts.DecisionTraces;

/// <summary>
/// Base type for coordinator vs authority traces. JSON uses <c>kind</c> plus either <c>runEvent</c> or <c>ruleAudit</c>
/// (<see cref="DecisionTraceJsonConverter"/>); CLR types are <see cref="RunEventTrace"/> or <see cref="RuleAuditTrace"/>.
/// </summary>
[JsonConverter(typeof(DecisionTraceJsonConverter))]
public abstract class DecisionTrace
{
    /// <summary>Pipeline discriminator; not duplicated in JSON when polymorphic serialization is used.</summary>
    public abstract DecisionTraceKind Kind { get; }

    /// <summary>Requires a coordinator merge/agent step trace.</summary>
    public RunEventTracePayload RequireRunEvent()
    {
        if (this is RunEventTrace runEvent)

            return runEvent.RunEvent;

        throw new InvalidOperationException("Expected a RunEvent trace (coordinator pipeline).");
    }

    /// <summary>Requires an authority rule-audit trace.</summary>
    public RuleAuditTracePayload RequireRuleAudit()
    {
        if (this is RuleAuditTrace ruleAudit)

            return ruleAudit.RuleAudit;

        throw new InvalidOperationException("Expected a RuleAudit trace (authority pipeline).");
    }
}
