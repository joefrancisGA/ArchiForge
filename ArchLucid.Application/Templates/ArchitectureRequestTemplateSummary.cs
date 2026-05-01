namespace ArchLucid.Application.Templates;

/// <summary>
///     Catalog metadata for a wizard-selectable architecture request template (read via
///     <c>GET /v1/architecture/templates</c>).
/// </summary>
public sealed record ArchitectureRequestTemplateSummary(string TemplateId, string Title, string ShortDescription);
