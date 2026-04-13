using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Raised when LLM output fails <see cref="ArchLucid.Decisioning.Validation.ISchemaValidationService.ValidateAgentResultJson"/> and enforcement is enabled.
/// </summary>
public sealed class AgentResultSchemaViolationException : InvalidOperationException
{
    /// <summary>Creates an exception with schema errors and context.</summary>
    public AgentResultSchemaViolationException(
        string message,
        IReadOnlyList<string> schemaErrors,
        string truncatedJson,
        AgentType agentType)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(schemaErrors);
        ArgumentException.ThrowIfNullOrWhiteSpace(truncatedJson);

        SchemaErrors = schemaErrors;
        TruncatedJson = truncatedJson;
        AgentType = agentType;
    }

    /// <summary>Human-readable schema validation messages.</summary>
    public IReadOnlyList<string> SchemaErrors { get; }

    /// <summary>Raw assistant JSON truncated for logs and exceptions (max ~2k chars).</summary>
    public string TruncatedJson { get; }

    /// <summary>Expected agent role for this parse.</summary>
    public AgentType AgentType { get; }
}
