namespace ArchLucid.Application.ExecDigest;

/// <summary>One committed manifest run highlighted in the digest (significance = proxy score from pilot deltas).</summary>
public sealed record ExecDigestHighlightedRun(string RunIdHex, int SignificanceScore, string? Caption);
