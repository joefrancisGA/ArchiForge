namespace ArchiForge.Application.Analysis;

public interface IComparisonDriftAnalyzer
{
    DriftAnalysisResult Analyze(
        object stored,
        object regenerated);
}
