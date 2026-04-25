using System.Globalization;
using System.Text;

using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Token-overlap heuristic: explanation word-like tokens are checked for substring presence in a flattened trace
///     corpus.
///     False negatives are common (paraphrases); false positives possible on short tokens shared with trace text.
/// </summary>
public sealed class ExplanationFaithfulnessChecker : IExplanationFaithfulnessChecker
{
    private const int MaxDistinctTokens = 400;
    private const int MinTokenLength = 4;
    private const int MaxUnsupportedListed = 32;

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "that",
        "this",
        "with",
        "from",
        "have",
        "has",
        "had",
        "were",
        "been",
        "will",
        "would",
        "could",
        "should",
        "must",
        "may",
        "might",
        "not",
        "are",
        "was",
        "and",
        "for",
        "the",
        "but",
        "any",
        "all",
        "each",
        "both",
        "such",
        "than",
        "then",
        "them",
        "their",
        "there",
        "these",
        "those",
        "into",
        "also",
        "only",
        "just",
        "more",
        "most",
        "some",
        "very",
        "when",
        "what",
        "which",
        "while",
        "where",
        "who",
        "how",
        "why",
        "your",
        "our",
        "its",
        "can",
        "did",
        "does",
        "done",
        "being",
        "over",
        "under",
        "after",
        "before",
        "between",
        "through",
        "during",
        "about",
        "against",
        "within",
        "without",
        "using",
        "based",
        "including",
        "included",
        "related",
        "overall",
        "several",
        "another",
        "other",
        "same",
        "well",
        "high",
        "low",
        "risk",
        "cost",
        "security",
        "compliance",
        "system",
        "design",
        "architecture",
        "manifest",
        "decision",
        "finding",
        "findings",
        "issue",
        "issues",
        "need",
        "needs",
        "must",
        "recommend",
        "summary"
    };

    /// <inheritdoc />
    public ExplanationFaithfulnessReport CheckFaithfulness(ExplanationResult explanation, FindingsSnapshot? snapshot)
    {
        ArgumentNullException.ThrowIfNull(explanation);

        if (snapshot is null || snapshot.Findings.Count == 0)
            return new ExplanationFaithfulnessReport(0, 0, 0, 1.0, []);


        string traceBlob = BuildTraceBlob(snapshot);
        string explanationBlob = BuildExplanationBlob(explanation);

        if (string.IsNullOrWhiteSpace(explanationBlob))
            return new ExplanationFaithfulnessReport(0, 0, 0, 1.0, []);


        HashSet<string> distinctTokens = CollectTokens(explanationBlob);

        if (distinctTokens.Count == 0)
            return new ExplanationFaithfulnessReport(0, 0, 0, 1.0, []);


        int supported = 0;
        List<string> unsupported = [];

        foreach (string token in distinctTokens)

            if (traceBlob.Contains(token, StringComparison.OrdinalIgnoreCase))

                supported++;

            else if (unsupported.Count < MaxUnsupportedListed)

                unsupported.Add(token);


        int checkedCount = distinctTokens.Count;
        int unsupportedCount = checkedCount - supported;
        double ratio = checkedCount > 0 ? (double)supported / checkedCount : 1.0;

        return new ExplanationFaithfulnessReport(
            checkedCount,
            supported,
            unsupportedCount,
            ratio,
            unsupported);
    }

    private static string BuildExplanationBlob(ExplanationResult r)
    {
        StringBuilder sb = new();

        Append(sb, r.Summary);
        Append(sb, r.DetailedNarrative);
        Append(sb, r.RawText);

        foreach (string line in r.KeyDrivers)

            Append(sb, line);


        foreach (string line in r.RiskImplications)

            Append(sb, line);


        foreach (string line in r.CostImplications)

            Append(sb, line);


        foreach (string line in r.ComplianceImplications)

            Append(sb, line);


        if (r.Structured is not null)

            Append(sb, r.Structured.Reasoning);


        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;


        sb.Append(' ');
        sb.Append(value);
    }

    private static string BuildTraceBlob(FindingsSnapshot snapshot)
    {
        StringBuilder sb = new();

        foreach (Finding f in snapshot.Findings)
        {
            Append(sb, f.FindingId);
            Append(sb, f.Title);
            Append(sb, f.Rationale);
            Append(sb, f.EngineType);
            Append(sb, f.FindingType);
            Append(sb, f.Category);

            foreach (string id in f.RelatedNodeIds)

                Append(sb, id);


            ExplainabilityTrace t = f.Trace;
            Append(sb, t.SourceAgentExecutionTraceId);

            foreach (string s in t.GraphNodeIdsExamined)

                Append(sb, s);


            foreach (string s in t.RulesApplied)

                Append(sb, s);


            foreach (string s in t.DecisionsTaken)

                Append(sb, s);


            foreach (string s in t.AlternativePathsConsidered)

                Append(sb, s);


            foreach (string s in t.Notes)

                Append(sb, s);
        }

        return sb.ToString().ToLowerInvariant();
    }

    private static HashSet<string> CollectTokens(string blob)
    {
        HashSet<string> tokens = new(StringComparer.OrdinalIgnoreCase);
        ReadOnlySpan<char> span = blob.AsSpan();
        int i = 0;

        while (i < span.Length && tokens.Count < MaxDistinctTokens)
        {
            while (i < span.Length && !char.IsLetterOrDigit(span[i]))

                i++;


            int start = i;

            while (i < span.Length && (char.IsLetterOrDigit(span[i]) || span[i] == '-' || span[i] == '_'))

                i++;


            int len = i - start;

            if (len < MinTokenLength)
                continue;


            string token = span.Slice(start, len).ToString();

            if (Stopwords.Contains(token))
                continue;


            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                continue;


            _ = tokens.Add(token);
        }

        return tokens;
    }
}
