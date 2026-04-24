namespace ArchLucid.Persistence.Coordination.Compare;

/// <summary>
///     String labels stored in <see cref="DiffItem.DiffKind" /> for comparison results and API responses.
/// </summary>
public static class DiffKind
{
    /// <summary>Item exists in the right-hand (target) but not in the left-hand (baseline).</summary>
    public const string Added = "Added";

    /// <summary>Item exists in the baseline but not in the target.</summary>
    public const string Removed = "Removed";

    /// <summary>Item exists in both but with different values.</summary>
    public const string Changed = "Changed";

    /// <summary>Item is identical in both sides (rarely emitted by comparer).</summary>
    public const string Unchanged = "Unchanged";
}
