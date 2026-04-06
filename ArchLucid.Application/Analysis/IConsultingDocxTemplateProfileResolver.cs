namespace ArchiForge.Application.Analysis;

/// <summary>
/// Provides the catalog of available consulting Docx template profiles.
/// </summary>
/// <remarks>
/// Implementations may return a static built-in catalog (see
/// <see cref="DefaultConsultingDocxTemplateProfileResolver"/>) or a dynamically
/// configured one loaded from configuration, a database, or a plugin registry.
/// </remarks>
public interface IConsultingDocxTemplateProfileResolver
{
    /// <summary>
    /// Returns all registered template profiles available for selection.
    /// </summary>
    /// <returns>
    /// A <see cref="ConsultingDocxTemplateProfileCatalog"/> containing at least one entry.
    /// </returns>
    ConsultingDocxTemplateProfileCatalog GetCatalog();
}
