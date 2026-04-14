using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Agents;

/// <summary>
/// Captures the full execution record for a single LLM call made by an agent during a run.
/// Traces are immutable after creation and are used for auditing, determinism verification,
/// and replay comparison.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sensitivity:</strong> <see cref="SystemPrompt"/> and <see cref="UserPrompt"/> may
/// contain architecture request details and policy summaries. <see cref="RawResponse"/> contains
/// the raw LLM output. Treat all three as internal data; do not expose directly in public API
/// responses without redaction.
/// </para>
/// <para>
/// <see cref="ParsedResultJson"/> is set only when <see cref="ParseSucceeded"/> is
/// <see langword="true"/>; consumers should check <see cref="ParseSucceeded"/> before
/// attempting to deserialize it.
/// </para>
/// </remarks>
public sealed class AgentExecutionTrace
{
    /// <summary>Unique identifier for this trace record.</summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The architecture run this trace belongs to.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>The agent task that triggered this LLM call.</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>The type of agent that executed this task.</summary>
    public AgentType AgentType { get; set; }

    /// <summary>The system prompt sent to the LLM for this call.</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>The user-turn prompt sent to the LLM for this call.</summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>The raw string response returned by the LLM.</summary>
    public string RawResponse { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized structured result parsed from <see cref="RawResponse"/>,
    /// or <see langword="null"/> when <see cref="ParseSucceeded"/> is <see langword="false"/>.
    /// </summary>
    public string? ParsedResultJson { get; set; }

    /// <summary>
    /// <see langword="true"/> when <see cref="RawResponse"/> was successfully parsed into a
    /// typed result; <see langword="false"/> when parsing failed (see <see cref="ErrorMessage"/>).
    /// </summary>
    public bool ParseSucceeded { get; set; }

    /// <summary>
    /// Error message recorded when <see cref="ParseSucceeded"/> is <see langword="false"/>,
    /// or <see langword="null"/> on success.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Stable catalog id for the system prompt template (e.g. <c>topology-system</c>).</summary>
    public string? PromptTemplateId { get; set; }

    /// <summary>Semantic version of the template content (bump when instructions change).</summary>
    public string? PromptTemplateVersion { get; set; }

    /// <summary>SHA-256 (hex, lowercase) of canonical UTF-8 system prompt bytes — use for regression detection and replay identity.</summary>
    public string? SystemPromptContentSha256 { get; set; }

    /// <summary>Optional operator-defined label from configuration (A/B variant, pilot name); not part of the content hash.</summary>
    public string? PromptReleaseLabel { get; set; }

    /// <summary>Prompt (input) token count from the provider, when reported.</summary>
    public int? InputTokenCount { get; set; }

    /// <summary>Completion (output) token count from the provider, when reported.</summary>
    public int? OutputTokenCount { get; set; }

    /// <summary>Optional estimated USD cost from input/output token counts when cost estimation is enabled in the recorder.</summary>
    public decimal? EstimatedCostUsd { get; set; }

    /// <summary>Blob store URI (or opaque pointer) for the unsanitized system prompt when full-trace persistence is enabled.</summary>
    public string? FullSystemPromptBlobKey { get; set; }

    /// <summary>Blob store URI for the unsanitized user prompt when full-trace persistence is enabled.</summary>
    public string? FullUserPromptBlobKey { get; set; }

    /// <summary>Blob store URI for the unsanitized raw model response when full-trace persistence is enabled.</summary>
    public string? FullResponseBlobKey { get; set; }

    /// <summary>Azure OpenAI deployment name (or provider equivalent) used for the call, when known.</summary>
    public string? ModelDeploymentName { get; set; }

    /// <summary>Provider-reported model version string, when available.</summary>
    public string? ModelVersion { get; set; }

    /// <summary>True when one or more full prompt/response blob uploads failed after all retries.</summary>
    public bool? BlobUploadFailed { get; set; }

    /// <summary>UTC timestamp when this trace was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
