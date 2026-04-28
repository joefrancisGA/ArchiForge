namespace ArchLucid.AgentRuntime;

/// <summary>
///     Controls how many sequential LLM completion attempts are permitted when structured output fails
///     <see cref="AgentResultSchemaViolationException" /> parsing / schema enforcement.
/// </summary>
/// <remarks>Configuration path: <see cref="SectionPath"/>.</remarks>
public sealed class AgentSchemaRemediationOptions
{
    /// <summary>Configuration path <c>AgentExecution:SchemaRemediation</c> (distinct from SchemaValidation JSON-schema paths).</summary>
    public const string SectionPath = "AgentExecution:SchemaRemediation";

    /// <summary>
    ///     Total completion attempts allowed for one handler invocation (minimum 1, capped at <see cref="MaxCompletionAttemptsCeiling"/>).
    ///     The first failure that is a schema violation triggers a follow-up completion with remediation instructions when this is &gt; 1.
    /// </summary>
    public int MaxCompletionAttempts
    {
        get;
        set;
    } = 3;

    /// <summary>Hard ceiling so misconfiguration cannot amplify spend without bound.</summary>
    public const int MaxCompletionAttemptsCeiling = 10;

    /// <summary>Clamps <see cref="MaxCompletionAttempts"/> into a safe interval.</summary>
    public void Normalize()
    {
        if (MaxCompletionAttempts < 1)
            MaxCompletionAttempts = 1;

        if (MaxCompletionAttempts > MaxCompletionAttemptsCeiling)
            MaxCompletionAttempts = MaxCompletionAttemptsCeiling;
    }
}
