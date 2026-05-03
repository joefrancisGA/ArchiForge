namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class LlmMonthlyTenantDollarBudgetRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("LlmMonthlyTenantDollarBudget:Enabled", false);

        if (!enabled)
            return;

        bool costEstimationEnabled = configuration.GetValue("AgentExecution:LlmCostEstimation:Enabled", true);

        if (!costEstimationEnabled)
            errors.Add(
                "AgentExecution:LlmCostEstimation:Enabled must be true when LlmMonthlyTenantDollarBudget:Enabled is true (USD budgets require token cost estimation).");

        decimal included = configuration.GetValue("LlmMonthlyTenantDollarBudget:IncludedUsdPerUtcMonth", 0m);

        if (included < 0.01m)
            errors.Add("LlmMonthlyTenantDollarBudget:IncludedUsdPerUtcMonth must be at least 0.01 when LlmMonthlyTenantDollarBudget:Enabled is true.");

        decimal hard = configuration.GetValue("LlmMonthlyTenantDollarBudget:HardCutoffUsdPerUtcMonth", 0m);

        if (hard < 0.01m)
            errors.Add("LlmMonthlyTenantDollarBudget:HardCutoffUsdPerUtcMonth must be at least 0.01 when LlmMonthlyTenantDollarBudget:Enabled is true.");

        if (hard < included)
            errors.Add(
                "LlmMonthlyTenantDollarBudget:HardCutoffUsdPerUtcMonth must be greater than or equal to LlmMonthlyTenantDollarBudget:IncludedUsdPerUtcMonth when LlmMonthlyTenantDollarBudget:Enabled is true.");

        decimal warn = configuration.GetValue("LlmMonthlyTenantDollarBudget:WarnFraction", 0.75m);

        if (warn is < 0.01m or > 0.99m)
            errors.Add("LlmMonthlyTenantDollarBudget:WarnFraction must be between 0.01 and 0.99 when LlmMonthlyTenantDollarBudget:Enabled is true.");

        int assumedPrompt = configuration.GetValue("LlmMonthlyTenantDollarBudget:AssumedMaxPromptTokensPerRequest", 32_768);

        if (assumedPrompt is < 1 or > 1_000_000)
            errors.Add(
                "LlmMonthlyTenantDollarBudget:AssumedMaxPromptTokensPerRequest must be between 1 and 1000000 when LlmMonthlyTenantDollarBudget:Enabled is true.");

        int assumedCompletion = configuration.GetValue("LlmMonthlyTenantDollarBudget:AssumedMaxCompletionTokensPerRequest", 8_192);

        if (assumedCompletion is < 1 or > 262_144)
            errors.Add(
                "LlmMonthlyTenantDollarBudget:AssumedMaxCompletionTokensPerRequest must be between 1 and 262144 when LlmMonthlyTenantDollarBudget:Enabled is true.");

        decimal inRate = configuration.GetValue("AgentExecution:LlmCostEstimation:InputUsdPerMillionTokens", 0.5m);
        decimal outRate = configuration.GetValue("AgentExecution:LlmCostEstimation:OutputUsdPerMillionTokens", 1.5m);

        if (inRate <= 0m || outRate <= 0m)
            errors.Add(
                "AgentExecution:LlmCostEstimation:InputUsdPerMillionTokens and OutputUsdPerMillionTokens must be positive when LlmMonthlyTenantDollarBudget:Enabled is true.");
    }
}
