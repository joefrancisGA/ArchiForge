namespace ArchiForge.Core.Configuration;

/// <summary>Per-tenant LLM token budgets over a sliding time window (prompt and completion tokens from Azure OpenAI usage).</summary>
public sealed class LlmTokenQuotaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LlmTokenQuota";

    /// <summary>When true, <see cref="ArchiForge.AgentRuntime.LlmCompletionAccountingClient"/> enforces limits before each completion call.</summary>
    public bool Enabled { get; set; }

    /// <summary>Sliding window length for quota accounting (1–1440 minutes).</summary>
    public int WindowMinutes { get; set; } = 60;

    /// <summary>Maximum sum of prompt (input) tokens per tenant in the window; 0 = no limit on prompt sum.</summary>
    public long MaxPromptTokensPerTenantPerWindow { get; set; }

    /// <summary>Maximum sum of completion (output) tokens per tenant in the window; 0 = no limit on completion sum.</summary>
    public long MaxCompletionTokensPerTenantPerWindow { get; set; }

    /// <summary>Upper bound assumed for a single request when checking quota before the model returns usage (prompt side).</summary>
    public int AssumedMaxPromptTokensPerRequest { get; set; } = 32_768;

    /// <summary>Upper bound assumed for a single request when checking quota before the model returns usage (completion side).</summary>
    public int AssumedMaxCompletionTokensPerRequest { get; set; } = 8_192;
}
