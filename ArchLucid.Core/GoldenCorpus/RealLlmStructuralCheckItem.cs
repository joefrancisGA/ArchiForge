namespace ArchLucid.Core.GoldenCorpus;

/// <summary>One named structural check (JSON shape, not semantic content).</summary>
public sealed record RealLlmStructuralCheckItem(string Name, bool Passed, string Message);
