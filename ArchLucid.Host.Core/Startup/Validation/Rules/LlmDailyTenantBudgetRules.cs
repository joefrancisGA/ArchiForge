namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class LlmDailyTenantBudgetRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("LlmDailyTenantBudget:Enabled", false);

        if (!enabled)
            return;

        long max = configuration.GetValue("LlmDailyTenantBudget:MaxTotalTokensPerTenantPerUtcDay", 0L);

        if (max < 1)
            errors.Add("LlmDailyTenantBudget:MaxTotalTokensPerTenantPerUtcDay must be at least 1 when LlmDailyTenantBudget:Enabled is true.");

        decimal warn = configuration.GetValue("LlmDailyTenantBudget:WarnFraction", 0.8m);

        if (warn is < 0.01m or > 0.99m)
            errors.Add("LlmDailyTenantBudget:WarnFraction must be between 0.01 and 0.99 when LlmDailyTenantBudget:Enabled is true.");

        int assumed = configuration.GetValue("LlmDailyTenantBudget:AssumedMaxTotalTokensPerRequest", 65_536);

        if (assumed is < 1 or > 2_000_000)
            errors.Add("LlmDailyTenantBudget:AssumedMaxTotalTokensPerRequest must be between 1 and 2000000 when LlmDailyTenantBudget:Enabled is true.");
    }
}
