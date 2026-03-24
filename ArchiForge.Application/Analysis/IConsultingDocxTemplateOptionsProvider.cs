namespace ArchiForge.Application.Analysis;

/// <summary>
/// Provides the active <see cref="ConsultingDocxTemplateOptions"/> for the current
/// consulting Docx export.
/// </summary>
public interface IConsultingDocxTemplateOptionsProvider
{
    /// <summary>
    /// Returns the resolved template options that control the report's content and appearance.
    /// </summary>
    ConsultingDocxTemplateOptions GetOptions();
}
