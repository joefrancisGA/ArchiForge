using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Summaries;

public sealed class DefaultContextDeltaSummaryBuilder : IContextDeltaSummaryBuilder
{
    public string BuildSegment(
        string connectorType,
        string baseSummary,
        NormalizedContextBatch batch,
        ContextSnapshot? previous,
        bool isFirstConnector)
    {
        int n = batch.CanonicalObjects.Count;
        string breakdown = n == 0
            ? "none"
            : string.Join(
                ", ",
                batch.CanonicalObjects
                    .GroupBy(o => o.ObjectType)
                    .OrderBy(g => g.Key, StringComparer.Ordinal)
                    .Select(g => $"{g.Key}×{g.Count()}"));

        string priorClause = "";
        if (isFirstConnector)
        {
            priorClause = previous is null
                ? " [baseline: no prior project snapshot]"
                : $" [baseline: prior snapshot had {previous.CanonicalObjects.Count} canonical object(s)]";
        }

        string head = string.IsNullOrWhiteSpace(baseSummary)
            ? connectorType
            : baseSummary.Trim();

        return $"{head}{priorClause} | {connectorType}: {n} produced ({breakdown})";
    }
}
