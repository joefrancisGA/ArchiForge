namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Defines the allowed single-step promotion path: dev → test → prod.
/// </summary>
public static class GovernanceEnvironmentOrder
{
    private static readonly IReadOnlyDictionary<string, int> Order =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [GovernanceEnvironment.Dev] = 0, [GovernanceEnvironment.Test] = 1, [GovernanceEnvironment.Prod] = 2
        };

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="target" /> is exactly one step after
    ///     <paramref name="source" /> in the governance ladder.
    /// </summary>
    public static bool IsValidPromotion(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return false;

        return Order.TryGetValue(source, out int s)
               && Order.TryGetValue(target, out int t)
               && t == s + 1;
    }
}
