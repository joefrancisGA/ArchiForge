using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.Compare;

/// <summary>
/// One row in a manifest or run comparison (section + key + kind + optional before/after values).
/// </summary>
/// <remarks>
/// <see cref="DiffKind"/> is typically one of <see cref="DiffKind.Added"/>, <see cref="DiffKind.Removed"/>, <see cref="DiffKind.Changed"/>, or <see cref="DiffKind.Unchanged"/> (unchanged is unused by current comparer).
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Comparison row DTO; no logic.")]
public class DiffItem
{
    /// <summary>Logical grouping (e.g. <c>Requirements</c>, <c>Security.Controls</c>, <c>Run</c>).</summary>
    public string Section { get; set; } = null!;

    /// <summary>Item identifier within the section (requirement name, control name, list value, etc.).</summary>
    public string Key { get; set; } = null!;

    /// <summary>Kind label; compare to <see cref="DiffKind"/> constants.</summary>
    public string DiffKind { get; set; } = null!;
    /// <summary>Left-hand value for <see cref="Compare.DiffKind.Changed"/> or <see cref="Compare.DiffKind.Removed"/> items; <see langword="null"/> for additions.</summary>
    public string? BeforeValue { get; set; }

    /// <summary>Right-hand value for <see cref="Compare.DiffKind.Changed"/> or <see cref="Compare.DiffKind.Added"/> items; <see langword="null"/> for removals.</summary>
    public string? AfterValue { get; set; }

    /// <summary>Optional explanation or contextual annotation attached by the comparer.</summary>
    public string? Notes { get; set; }
}
