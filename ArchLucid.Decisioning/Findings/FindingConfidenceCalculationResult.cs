using ArchLucid.Contracts.Findings;

namespace ArchLucid.Decisioning.Findings;

/// <summary>0–100 gate-derived score plus mapped <see cref="FindingConfidenceLevel" />.</summary>
public sealed record FindingConfidenceCalculationResult(int Score, FindingConfidenceLevel Level);
