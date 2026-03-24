namespace ArchiForge.Application.Analysis;

/// <summary>
/// A catalog of all registered consulting Docx template profiles returned by
/// <see cref="IConsultingDocxTemplateProfileResolver.GetCatalog"/>.
/// </summary>
public sealed class ConsultingDocxTemplateProfileCatalog
{
    /// <summary>
    /// All profiles available for selection, in no guaranteed order.
    /// Ordered collections should sort by <see cref="ConsultingDocxTemplateProfileInfo.DisplayOrder"/>.
    /// </summary>
    public List<ConsultingDocxTemplateProfileInfo> Profiles { get; set; } = [];
}

/// <summary>
/// Lightweight descriptor for a consulting Docx template profile registered in
/// <see cref="ConsultingDocxTemplateProfileCatalog"/>.
/// </summary>
public sealed class ConsultingDocxTemplateProfileInfo
{
    /// <summary>
    /// Machine-readable profile identifier used as the lookup key (e.g. <c>executive</c>).
    /// See <see cref="ConsultingDocxProfiles"/> for well-known values.
    /// </summary>
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label shown in UIs and report headers (e.g. <c>Executive Brief</c>).
    /// </summary>
    public string ProfileDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Relative sort order for displaying profiles in a list. Lower values appear first.
    /// Defaults to <c>100</c>.
    /// </summary>
    public int DisplayOrder { get; set; } = 100;
}
