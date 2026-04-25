namespace ArchLucid.ArtifactSynthesis.Tests.GoldenCorpus;

internal static class SynthesisGoldenCorpusPaths
{
    internal static string CorpusOutputDirectory =>
        Path.Combine(AppContext.BaseDirectory, "golden-corpus", "synthesis");
}
