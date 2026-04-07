namespace ArchLucid.Core.Explanation;

/// <summary>
/// Machine-readable explanation envelope. Free-text LLM output is normalized into this shape
/// so consumers can inspect reasoning, evidence, confidence, and caveats without parsing prose.
/// </summary>
public sealed record StructuredExplanation
{
    /// <summary>Schema version for forward compatibility (bump on breaking field changes).</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Primary explanatory text (populated from structured JSON or raw LLM text when parse fails).</summary>
    public required string Reasoning { get; init; }

    /// <summary>IDs or keys of provenance / evidence nodes cited in the reasoning.</summary>
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    /// <summary>Model-estimated confidence in the explanation, 0.0–1.0. Null when not computable.</summary>
    public decimal? Confidence { get; init; }

    /// <summary>Other options the system evaluated before choosing this explanation path.</summary>
    public IReadOnlyList<string>? AlternativesConsidered { get; init; }

    /// <summary>Known limitations or assumptions baked into this explanation.</summary>
    public IReadOnlyList<string>? Caveats { get; init; }
}
