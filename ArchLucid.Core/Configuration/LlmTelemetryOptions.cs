namespace ArchLucid.Core.Configuration;

/// <summary>Optional high-cardinality LLM metrics (tenant id label). Use only when tenant count is bounded.</summary>
public sealed class LlmTelemetryOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LlmTelemetry";

    /// <summary>
    ///     When true, prompt/completion counters also emit with <c>tenant_id</c> tag (in addition to aggregate series
    ///     without tenant).
    /// </summary>
    public bool RecordPerTenantTokens
    {
        get;
        set;
    }
}
