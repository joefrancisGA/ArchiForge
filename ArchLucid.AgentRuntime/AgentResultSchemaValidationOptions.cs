namespace ArchLucid.AgentRuntime;

/// <summary>
/// Options for JSON schema validation of raw LLM <see cref="ArchLucid.Contracts.Agents.AgentResult"/> payloads before domain checks.
/// </summary>
public sealed class AgentResultSchemaValidationOptions
{
    /// <summary>Configuration path: <c>AgentExecution:SchemaValidation</c>.</summary>
    public const string SectionPath = "AgentExecution:SchemaValidation";

    /// <summary>
    /// When <see langword="true"/> (default), <see cref="AgentResultParser"/> throws <see cref="AgentResultSchemaViolationException"/>
    /// if <see cref="ArchLucid.Decisioning.Validation.ISchemaValidationService.ValidateAgentResultJson"/> reports errors.
    /// When <see langword="false"/>, violations are logged as warnings and parsing continues.
    /// </summary>
    public bool EnforceOnParse { get; set; } = true;
}
