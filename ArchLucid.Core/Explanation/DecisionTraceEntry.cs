namespace ArchLucid.Core.Explanation;

/// <summary>Normalized decision trace row for rule-audit (authority) or run-event (coordinator) payloads.</summary>
public sealed class DecisionTraceEntry
{
    public string TraceId
    {
        get;
        set;
    } = string.Empty;

    public DateTimeOffset CreatedUtc
    {
        get;
        set;
    }

    /// <summary><c>ruleAudit</c> or <c>runEvent</c>.</summary>
    public string Kind
    {
        get;
        set;
    } = string.Empty;

    public string Description
    {
        get;
        set;
    } = string.Empty;

    public Dictionary<string, object> Details
    {
        get;
        set;
    } = new();
}
