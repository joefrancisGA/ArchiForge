using ArchLucid.Persistence.Coordination.Caching;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class HotPathCacheRules
{
    public static void Collect(IConfiguration configuration, IWebHostEnvironment environment, List<string> errors)
    {
        HotPathCacheOptions opts =
            configuration.GetSection(HotPathCacheOptions.SectionName).Get<HotPathCacheOptions>() ??
            new HotPathCacheOptions();

        if (!opts.Enabled)
        {
            return;
        }

        string provider = opts.Provider;

        if (!string.Equals(provider, "Memory", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(
                "HotPathCache:Provider must be 'Memory', 'Redis', or 'Auto' when HotPathCache:Enabled is true.");
        }

        if (string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(opts.RedisConnectionString))
        {
            errors.Add("HotPathCache:RedisConnectionString is required when HotPathCache:Provider is Redis.");
        }

        if (string.Equals(provider, "Auto", StringComparison.OrdinalIgnoreCase) &&
            opts.ExpectedApiReplicaCount > 1 &&
            string.IsNullOrWhiteSpace(opts.RedisConnectionString) &&
            !environment.IsDevelopment())
        {
            errors.Add(
                "HotPathCache:RedisConnectionString is required when HotPathCache:Provider is Auto and HotPathCache:ExpectedApiReplicaCount is greater than 1 outside Development (distributed cache across replicas).");
        }

        if (opts.AbsoluteExpirationSeconds > 3600)
        {
            errors.Add("HotPathCache:AbsoluteExpirationSeconds cannot exceed 3600.");
        }
    }
}
