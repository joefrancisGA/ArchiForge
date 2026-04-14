namespace ArchLucid.Contracts.Governance;

/// <summary>Outcome of evaluating whether manifest commit is allowed under governance policy.</summary>
public sealed class PreCommitGateResult
{
    public bool Blocked { get; init; }

    public string? Reason { get; init; }

    public IReadOnlyList<string> BlockingFindingIds { get; init; } = Array.Empty<string>();

    /// <summary>Policy pack identifier (string form) that enforced the gate, when applicable.</summary>
    public string? PolicyPackId { get; init; }

    /// <summary>The effective minimum severity (ordinal from <c>FindingSeverity</c>) that triggered the block, when applicable.</summary>
    public int? MinimumBlockingSeverity { get; init; }

    /// <summary>When true, findings met the threshold but the severity is in the warn-only list; commit is allowed.</summary>
    public bool WarnOnly { get; init; }

    /// <summary>Warning messages when <see cref="WarnOnly"/> is true.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static PreCommitGateResult Allowed() => new() { Blocked = false };
}
