namespace ArchLucid.ContextIngestion.Tests.GoldenCorpus;

internal static class IngestionGoldenCorpusPaths
{
    internal static string CorpusOutputDirectory =>
        Path.Combine(AppContext.BaseDirectory, "golden-corpus", "ingestion");
}
