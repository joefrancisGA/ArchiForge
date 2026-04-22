namespace ArchLucid.Core.Configuration;

/// <summary>String tokens for <see cref="TrialAuthOptions.Modes" />.</summary>
public static class TrialAuthModeConstants
{
    public const string MsaExternalId = "MsaExternalId";
    public const string LocalIdentity = "LocalIdentity";

    public static bool HasMode(IReadOnlyCollection<string>? modes, string mode)
    {
        if (modes is null || modes.Count == 0 || string.IsNullOrWhiteSpace(mode))
            return false;

        return modes.Any(m => string.Equals(m.Trim(), mode, StringComparison.OrdinalIgnoreCase));
    }
}
