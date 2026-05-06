namespace ArchLucid.Application.Templates;
/// <summary>
///     Catalog metadata for a wizard-selectable architecture request template (read via
///     <c>GET /v1/architecture/templates</c>).
/// </summary>
public sealed record ArchitectureRequestTemplateSummary(string TemplateId, string Title, string ShortDescription)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(TemplateId, Title, ShortDescription);
    private static byte __ValidatePrimaryConstructorArguments(System.String TemplateId, System.String Title, System.String ShortDescription)
    {
        ArgumentNullException.ThrowIfNull(TemplateId);
        ArgumentNullException.ThrowIfNull(Title);
        ArgumentNullException.ThrowIfNull(ShortDescription);
        return (byte)0;
    }
}