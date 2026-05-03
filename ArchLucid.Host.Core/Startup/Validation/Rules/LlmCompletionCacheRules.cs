using ArchLucid.Persistence.Coordination.Caching;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class LlmCompletionCacheRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("LlmCompletionCache:Enabled", true);

        if (!enabled)
            return;

        int maxEntries = configuration.GetValue("LlmCompletionCache:MaxEntries", 256);

        if (maxEntries is < 1 or > 100_000)

            errors.Add(
                "LlmCompletionCache:MaxEntries must be between 1 and 100000 when LlmCompletionCache:Enabled is true.");

        int ttlSeconds = configuration.GetValue("LlmCompletionCache:AbsoluteExpirationSeconds", 600);

        if (ttlSeconds is < 1 or > 604_800)

            errors.Add(
                "LlmCompletionCache:AbsoluteExpirationSeconds must be between 1 and 604800 when LlmCompletionCache:Enabled is true.");

        string? provider = configuration["LlmCompletionCache:Provider"]?.Trim();

        if (!string.IsNullOrEmpty(provider) &&
            !string.Equals(provider, "Memory", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Distributed", StringComparison.OrdinalIgnoreCase))

            errors.Add("LlmCompletionCache:Provider must be 'Memory' or 'Distributed' when set.");

        if (!string.Equals(provider, "Distributed", StringComparison.OrdinalIgnoreCase))
            return;

        string? llmRedis = configuration["LlmCompletionCache:RedisConnectionString"]?.Trim();
        string? hotRedis = configuration["HotPathCache:RedisConnectionString"]?.Trim();
        HotPathCacheOptions hotOpts =
            configuration.GetSection(HotPathCacheOptions.SectionName).Get<HotPathCacheOptions>() ??
            new HotPathCacheOptions();
        bool hotPathUsesRedis = hotOpts.Enabled &&
                                string.Equals(
                                    HotPathCacheProviderResolver.ResolveEffectiveProvider(hotOpts),
                                    "Redis",
                                    StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(llmRedis) && string.IsNullOrEmpty(hotRedis) && !hotPathUsesRedis)

            errors.Add(
                "LlmCompletionCache:Provider Distributed requires LlmCompletionCache:RedisConnectionString, or HotPathCache:RedisConnectionString with HotPathCache configured for Redis, so the host can register IDistributedCache.");
    }
}
