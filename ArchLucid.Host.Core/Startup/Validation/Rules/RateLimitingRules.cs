using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class RateLimitingRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        IConfigurationSection fixedSection = configuration.GetSection("RateLimiting:FixedWindow");

        if (fixedSection.Exists())
        {
            int permit = configuration.GetValue(
                "RateLimiting:FixedWindow:PermitLimit",
                RateLimitingDefaults.FixedWindowPermitLimit);
            int window = configuration.GetValue("RateLimiting:FixedWindow:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:FixedWindow:QueueLimit", 0);
            AddIfInvalid(errors, "RateLimiting:FixedWindow", permit, window, queue);
        }

        IConfigurationSection expensiveSection = configuration.GetSection("RateLimiting:Expensive");

        if (expensiveSection.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:Expensive:PermitLimit", 20);
            int window = configuration.GetValue("RateLimiting:Expensive:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:Expensive:QueueLimit", 0);
            AddIfInvalid(errors, "RateLimiting:Expensive", permit, window, queue);
        }

        IConfigurationSection replayLight = configuration.GetSection("RateLimiting:Replay:Light");

        if (replayLight.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:Replay:Light:PermitLimit", 60);
            int window = configuration.GetValue("RateLimiting:Replay:Light:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:Replay:Light:QueueLimit", 0);
            AddIfInvalid(errors, "RateLimiting:Replay:Light", permit, window, queue);
        }

        IConfigurationSection replayHeavy = configuration.GetSection("RateLimiting:Replay:Heavy");

        if (replayHeavy.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:Replay:Heavy:PermitLimit", 15);
            int window = configuration.GetValue("RateLimiting:Replay:Heavy:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:Replay:Heavy:QueueLimit", 0);
            AddIfInvalid(errors, "RateLimiting:Replay:Heavy", permit, window, queue);
        }

        IConfigurationSection governanceDryRun = configuration.GetSection("RateLimiting:GovernancePolicyPackDryRun");

        if (!governanceDryRun.Exists())
            return;

        {
            int permit = configuration.GetValue(
                "RateLimiting:GovernancePolicyPackDryRun:PermitLimit",
                RateLimitingDefaults.GovernancePolicyPackDryRunPermitLimit);
            int window = configuration.GetValue("RateLimiting:GovernancePolicyPackDryRun:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:GovernancePolicyPackDryRun:QueueLimit", 0);
            AddIfInvalid(errors, "RateLimiting:GovernancePolicyPackDryRun", permit, window, queue);
        }
    }

    private static void AddIfInvalid(List<string> errors, string path, int permitLimit, int windowMinutes, int queueLimit)
    {
        if (permitLimit < 1)

            errors.Add($"{path}:PermitLimit must be at least 1.");

        if (windowMinutes < 1)

            errors.Add($"{path}:WindowMinutes must be at least 1.");

        if (queueLimit < 0)

            errors.Add($"{path}:QueueLimit must be 0 or greater.");
    }
}
