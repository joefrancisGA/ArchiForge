namespace ArchiForge.Api.Validators;

/// <summary>Shared allowed values for replay comparison request validation (format, replayMode, profile).</summary>
internal static class ReplayValidationConstants
{
    public static readonly HashSet<string> ValidFormats =
        new(StringComparer.OrdinalIgnoreCase) { "markdown", "html", "docx", "json" };

    public static readonly HashSet<string> ValidReplayModes =
        new(StringComparer.OrdinalIgnoreCase) { "artifact", "regenerate", "verify" };

    public static readonly HashSet<string> ValidProfiles =
        new(StringComparer.OrdinalIgnoreCase) { "default", "short", "detailed", "executive" };
}
