using System.Globalization;
using System.Text;

using ArchLucid.Contracts.Explanation;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Builds a deterministic, human-readable narrative from <see cref="ExplainabilityTrace" /> fields (no LLM).
/// </summary>
public static class FindingExplainabilityNarrativeBuilder
{
    /// <summary>
    ///     Builds the structured factual explainability record from persisted <see cref="Finding" /> +
    ///     <see cref="ExplainabilityTrace" /> (no LLM).
    /// </summary>
    public static FindingExplainabilityEvidence BuildEvidence(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        ExplainabilityTrace trace = finding.Trace;

        List<string> evidenceRefs = CollectEvidenceRefs(finding, trace);
        List<string> alternativePaths = CollectNonEmptyTrimmed(trace.AlternativePathsConsidered);
        string ruleId = ResolveRuleId(trace);
        string conclusion = finding.Rationale;

        return new FindingExplainabilityEvidence(evidenceRefs, conclusion, alternativePaths, ruleId);
    }

    /// <summary>
    ///     Composes plain text suitable for UI or API consumers; never returns <see langword="null" /> (empty string when
    ///     nothing to say).
    /// </summary>
    public static string Build(
        string findingId,
        string title,
        string engineType,
        ExplainabilityTrace trace,
        double traceCompletenessRatio)
    {
        return Build(findingId, title, engineType, trace, traceCompletenessRatio, null);
    }

    /// <summary>
    ///     Same as <see cref="Build(string,string,string,ExplainabilityTrace,double)" />, but resolves graph node ids to
    ///     <c>Label (id)</c> when <paramref name="graphNodeLabels" /> contains a non-empty entry for the id.
    /// </summary>
    public static string Build(
        string findingId,
        string title,
        string engineType,
        ExplainabilityTrace trace,
        double traceCompletenessRatio,
        IReadOnlyDictionary<string, string>? graphNodeLabels)
    {
        ArgumentNullException.ThrowIfNull(trace);

        StringBuilder sb = new();

        AppendHeader(sb, findingId, title, engineType, traceCompletenessRatio);
        AppendOptionalLine(sb, "Source agent execution trace id", trace.SourceAgentExecutionTraceId);
        AppendGraphNodeBulletSection(sb, trace.GraphNodeIdsExamined, graphNodeLabels);
        AppendBulletSection(sb, "Rules applied", trace.RulesApplied);
        AppendBulletSection(sb, "Decisions taken", trace.DecisionsTaken);
        AppendBulletSection(sb, "Alternative paths considered", trace.AlternativePathsConsidered);
        AppendBulletSection(sb, "Notes", trace.Notes);

        return sb.Length == 0 ? string.Empty : sb.ToString().TrimEnd();
    }

    private static List<string> CollectEvidenceRefs(Finding finding, ExplainabilityTrace trace)
    {
        List<string> refs = [];

        AppendDistinctNonEmpty(refs, trace.GraphNodeIdsExamined);
        AppendDistinctNonEmpty(refs, finding.RelatedNodeIds);

        string? agentTraceId = trace.SourceAgentExecutionTraceId;

        if (string.IsNullOrWhiteSpace(agentTraceId))
            return refs;

        string agentRef = $"agentExecutionTrace:{agentTraceId.Trim()}";

        if (!refs.Contains(agentRef, StringComparer.Ordinal))
            refs.Add(agentRef);

        return refs;
    }

    private static void AppendDistinctNonEmpty(List<string> refs, IEnumerable<string>? candidates)
    {
        if (candidates is null)
            return;

        foreach (string raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            string trimmed = raw.Trim();

            if (refs.Contains(trimmed, StringComparer.Ordinal))
                continue;

            refs.Add(trimmed);
        }
    }

    private static List<string> CollectNonEmptyTrimmed(IEnumerable<string>? items)
    {
        if (items is null)
            return [];

        return items
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => s.Trim())
            .ToList();
    }

    private static string ResolveRuleId(ExplainabilityTrace trace)
    {
        List<string> rules = CollectNonEmptyTrimmed(trace.RulesApplied);

        if (rules.Count == 0)
            return "unspecified";

        return rules.Count == 1 ? rules[0] : string.Join(";", rules);
    }

    private static void AppendHeader(
        StringBuilder sb,
        string findingId,
        string title,
        string engineType,
        double traceCompletenessRatio)
    {
        if (string.IsNullOrWhiteSpace(findingId) && string.IsNullOrWhiteSpace(title))
            return;

        string idPart = string.IsNullOrWhiteSpace(findingId) ? "Finding" : $"Finding {findingId}";
        string titlePart = string.IsNullOrWhiteSpace(title) ? string.Empty : $": {title}";
        string enginePart = string.IsNullOrWhiteSpace(engineType) ? string.Empty : $" (engine: {engineType})";

        sb.Append(idPart);
        sb.Append(titlePart);
        sb.Append(enginePart);
        sb.AppendLine();

        sb.Append("Explainability trace completeness: ");
        sb.Append(traceCompletenessRatio.ToString("P0", CultureInfo.InvariantCulture));
        sb.AppendLine();
        sb.AppendLine();
    }

    private static void AppendOptionalLine(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        sb.Append(label);
        sb.Append(": ");
        sb.Append(value);
        sb.AppendLine();
    }

    private static void AppendGraphNodeBulletSection(
        StringBuilder sb,
        IReadOnlyList<string>? nodeIds,
        IReadOnlyDictionary<string, string>? labelsById)
    {
        if (nodeIds is null || nodeIds.Count == 0)
            return;

        List<string> nonEmpty = nodeIds
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => s.Trim())
            .ToList();

        if (nonEmpty.Count == 0)
            return;

        sb.Append("Graph nodes examined");
        sb.AppendLine();

        foreach (string id in nonEmpty)
        {
            string line = id;

            if (labelsById is not null
                && labelsById.TryGetValue(id, out string? label)
                && !string.IsNullOrWhiteSpace(label))

                line = $"{label.Trim()} ({id})";

            sb.Append("- ");
            sb.Append(line);
            sb.AppendLine();
        }

        sb.AppendLine();
    }

    private static void AppendBulletSection(StringBuilder sb, string heading, IReadOnlyList<string>? items)
    {
        if (items is null || items.Count == 0)
            return;

        List<string> nonEmpty = items
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => s.Trim())
            .ToList();

        if (nonEmpty.Count == 0)
            return;

        sb.Append(heading);
        sb.AppendLine();

        foreach (string line in nonEmpty)
        {
            sb.Append("- ");
            sb.Append(line);
            sb.AppendLine();
        }

        sb.AppendLine();
    }
}
