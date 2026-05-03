namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class LlmTokenQuotaRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("LlmTokenQuota:Enabled", false);

        if (!enabled)
            return;

        int windowMinutes = configuration.GetValue("LlmTokenQuota:WindowMinutes", 60);

        if (windowMinutes is < 1 or > 1440)

            errors.Add("LlmTokenQuota:WindowMinutes must be between 1 and 1440 when LlmTokenQuota:Enabled is true.");

        long maxPrompt = configuration.GetValue<long>("LlmTokenQuota:MaxPromptTokensPerTenantPerWindow", 0);
        long maxCompletion = configuration.GetValue<long>("LlmTokenQuota:MaxCompletionTokensPerTenantPerWindow", 0);

        if (maxPrompt < 1 && maxCompletion < 1)

            errors.Add(
                "When LlmTokenQuota:Enabled is true, set at least one of LlmTokenQuota:MaxPromptTokensPerTenantPerWindow or LlmTokenQuota:MaxCompletionTokensPerTenantPerWindow to a positive value.");

        int assumedPrompt = configuration.GetValue("LlmTokenQuota:AssumedMaxPromptTokensPerRequest", 32_768);

        if (assumedPrompt is < 1 or > 1_000_000)

            errors.Add(
                "LlmTokenQuota:AssumedMaxPromptTokensPerRequest must be between 1 and 1000000 when LlmTokenQuota:Enabled is true.");

        int assumedCompletion = configuration.GetValue("LlmTokenQuota:AssumedMaxCompletionTokensPerRequest", 8_192);

        if (assumedCompletion is < 1 or > 262_144)

            errors.Add(
                "LlmTokenQuota:AssumedMaxCompletionTokensPerRequest must be between 1 and 262144 when LlmTokenQuota:Enabled is true.");
    }
}
