using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for <see cref="ArchiForge.Persistence.Compare.DiffItem"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API contract DTO; no business logic.")]
public class DiffItemResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Compare.DiffItem.Section"/>
    public string Section { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.DiffItem.Key"/>
    public string Key { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.DiffItem.DiffKind"/>
    public string DiffKind { get; set; } = null!;
    /// <inheritdoc cref="ArchiForge.Persistence.Compare.DiffItem.BeforeValue"/>
    public string? BeforeValue { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.DiffItem.AfterValue"/>
    public string? AfterValue { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.DiffItem.Notes"/>
    public string? Notes { get; set; }
}
