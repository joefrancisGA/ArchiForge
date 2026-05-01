namespace ArchLucid.Application.Analysis;

/// <summary>
///     The outcome of <see cref="IConsultingDocxExportProfileSelector.Resolve" /> describing
///     which Docx template profile will be used for an export and how that decision was made.
/// </summary>
public sealed class ResolvedConsultingDocxExportProfile
{
    /// <summary>
    ///     Machine-readable name of the selected profile (e.g. <c>client</c>, <c>regulated</c>).
    /// </summary>
    public string SelectedProfileName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Human-readable display name of the selected profile (e.g. <c>Client Delivery Report</c>).
    /// </summary>
    public string SelectedProfileDisplayName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     <see langword="true" /> when the profile was inferred automatically from context signals;
    ///     <see langword="false" /> when the caller supplied an explicit profile name.
    /// </summary>
    public bool WasAutoSelected
    {
        get;
        set;
    }

    /// <summary>
    ///     Plain-language explanation of why this profile was selected.
    /// </summary>
    public string ResolutionReason
    {
        get;
        set;
    } = string.Empty;
}
