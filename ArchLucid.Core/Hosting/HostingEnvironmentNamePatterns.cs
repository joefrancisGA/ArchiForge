namespace ArchLucid.Core.Hosting;

/// <summary>
///     Shared interpretation of environment names that should be treated as production-like for bypass-auth guards
///     and operational startup hints (see <see cref="ProductionLikeHostingMisconfigurationAdvisor" />).
/// </summary>
public static class HostingEnvironmentNamePatterns
{
    /// <summary>
    ///     Treats names containing <c>prod</c> (case-insensitive) as production-like so misnamed hosts
    ///     (for example <c>PreProduction</c>, <c>staging-prod</c>) cannot rely on Development-only behaviour.
    ///     Excludes <c>non-production</c> / <c>nonproduction</c>.
    /// </summary>
    public static bool EnvironmentNameImpliesProductionLike(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
            return false;

        string trimmed = environmentName.Trim();

        if (trimmed.Contains("non-production", StringComparison.OrdinalIgnoreCase))
            return false;

        return !trimmed.Contains("nonproduction", StringComparison.OrdinalIgnoreCase)
            && trimmed.Contains("prod", StringComparison.OrdinalIgnoreCase);
    }
}
