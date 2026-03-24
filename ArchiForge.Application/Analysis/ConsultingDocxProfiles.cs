namespace ArchiForge.Application.Analysis;

/// <summary>
/// Well-known consulting Docx template profile names used across the recommendation
/// and resolution pipeline.
/// </summary>
/// <remarks>
/// These constants match the <see cref="ConsultingDocxTemplateProfileInfo.ProfileName"/> values
/// registered in <see cref="DefaultConsultingDocxTemplateProfileResolver"/>. Any resolver that
/// adds custom profiles should still honour these names when present.
/// </remarks>
public static class ConsultingDocxProfiles
{
    /// <summary>High-level stakeholder brief with minimal technical depth.</summary>
    public const string Executive = "executive";

    /// <summary>Compliance-heavy profile with governance appendices and control tables.</summary>
    public const string Regulated = "regulated";

    /// <summary>Balanced external-delivery profile suitable for client-facing reports.</summary>
    public const string Client = "client";

    /// <summary>Full-depth internal technical review with traces and determinism appendices.</summary>
    public const string Internal = "internal";
}
