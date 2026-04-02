using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;

namespace ArchiForge.Persistence.ProductLearning.Planning;

/// <inheritdoc />
public sealed class ImprovementThemeExtractionService : IImprovementThemeExtractionService
{
    public Task<IReadOnlyList<ImprovementThemeWithEvidence>> ExtractThemesAsync(
        ProductLearningAggregationSnapshot snapshot,
        IReadOnlyList<ProductLearningPilotSignalRecord> scopedSignals,
        IReadOnlyList<TriageQueueItem>? triageQueue,
        ImprovementThemeExtractionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scopedSignals);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        ProductLearningScope scope = snapshot.Scope;

        EnsureScopeMatchesSignals(scope, scopedSignals);

        int maxThemes = options.MaxThemes < 1 ? 1 : Math.Min(options.MaxThemes, 500);

        List<ProductLearningPilotSignalRecord> orderedSignals = scopedSignals
            .OrderByDescending(static r => r.RecordedUtc)
            .ThenBy(static r => r.SignalId)
            .ToList();

        Dictionary<string, ThemeAccumulator> buckets = new(StringComparer.Ordinal);

        AddRollupThemes(snapshot, options, buckets);
        AddTrendThemes(snapshot, options, buckets);
        AddCommentThemes(snapshot, options, buckets);
        AddTagThemes(orderedSignals, options, buckets);

        List<ImprovementThemeWithEvidence> results = Materialize(
            scope,
            buckets,
            orderedSignals,
            options);

        ApplyTriageSignalHints(results, triageQueue, orderedSignals, options);

        List<ImprovementThemeWithEvidence> ranked = results
            .OrderByDescending(static r => r.Theme.EvidenceCount)
            .ThenBy(static r => r.CanonicalKey, StringComparer.Ordinal)
            .Take(maxThemes)
            .ToList();

        return Task.FromResult<IReadOnlyList<ImprovementThemeWithEvidence>>(ranked);
    }

    private static void EnsureScopeMatchesSignals(
        ProductLearningScope scope,
        IReadOnlyList<ProductLearningPilotSignalRecord> scopedSignals)
    {
        foreach (ProductLearningPilotSignalRecord row in scopedSignals)
        {
            if (row.TenantId != scope.TenantId ||
                row.WorkspaceId != scope.WorkspaceId ||
                row.ProjectId != scope.ProjectId)
            {
                throw new ArgumentException(
                    "scopedSignals must match snapshot.Scope tenant/workspace/project for every row.",
                    nameof(scopedSignals));
            }
        }
    }

    private static void AddRollupThemes(
        ProductLearningAggregationSnapshot snapshot,
        ImprovementThemeExtractionOptions options,
        Dictionary<string, ThemeAccumulator> buckets)
    {
        foreach (FeedbackAggregate aggregate in snapshot.FeedbackRollups)
        {
            if (!PassesAggregateOutcomeGate(aggregate, options))
            {
                continue;
            }

            string canonicalKey = "rollup:" + aggregate.AggregateKey;

            if (buckets.ContainsKey(canonicalKey))
            {
                continue;
            }

            string name = BuildRollupName(aggregate);
            string description = BuildRollupDescription(aggregate);

            buckets[canonicalKey] = new ThemeAccumulator
            {
                CanonicalKey = canonicalKey,
                GroupingExplanation =
                    "Run / workflow feedback grouped by the same aggregate key as the dashboard (pattern key, else subject+artifact hint), "
                    + "when rejected, needs-follow-up, or revised counts cross configured thresholds. "
                    + "Disposition “Revised” maps to pilot “needs revision” outcomes.",
                Name = name,
                Description = description,
                EvidenceCount = aggregate.TotalSignalCount,
                AffectedArtifactTypes = CollectAffectedFromAggregate(aggregate),
                FirstSeenUtc = aggregate.FirstSignalRecordedUtc,
                LastSeenUtc = aggregate.LastSignalRecordedUtc,
            };
        }
    }

    private static void AddTrendThemes(
        ProductLearningAggregationSnapshot snapshot,
        ImprovementThemeExtractionOptions options,
        Dictionary<string, ThemeAccumulator> buckets)
    {
        foreach (ArtifactOutcomeTrend trend in snapshot.ArtifactTrends)
        {
            if (!PassesTrendGate(trend, options))
            {
                continue;
            }

            string canonicalKey = "trend:" + trend.TrendKey;

            if (buckets.ContainsKey(canonicalKey))
            {
                continue;
            }

            int total = TotalTrendSignals(trend);
            int negative = trend.RejectionCount + trend.RevisionCount + trend.NeedsFollowUpCount;

            buckets[canonicalKey] = new ThemeAccumulator
            {
                CanonicalKey = canonicalKey,
                GroupingExplanation =
                    "Artifact-level outcome mix grouped by subject type + artifact hint (trend key), "
                    + "when negative outcomes (reject / revised / follow-up) repeat beyond configured thresholds.",
                Name = "Artifact outcomes: " + Truncate(trend.ArtifactTypeOrHint, 200),
                Description =
                    $"Artifact trend key={trend.TrendKey}; totalSignals={total}; negativeOutcomes={negative} "
                    + $"(rejected={trend.RejectionCount}, revised={trend.RevisionCount}, followUp={trend.NeedsFollowUpCount}); "
                    + $"trustedOrAccepted={trend.AcceptedOrTrustedCount}; distinctRuns={trend.DistinctRunCount}.",
                EvidenceCount = total,
                AffectedArtifactTypes = new[] { trend.ArtifactTypeOrHint },
                FirstSeenUtc = trend.FirstSeenUtc,
                LastSeenUtc = trend.LastSeenUtc,
            };
        }
    }

    private static void AddCommentThemes(
        ProductLearningAggregationSnapshot snapshot,
        ImprovementThemeExtractionOptions options,
        Dictionary<string, ThemeAccumulator> buckets)
    {
        int min = options.MinCommentOccurrences < 1 ? 1 : options.MinCommentOccurrences;

        foreach (RepeatedCommentTheme theme in snapshot.RepeatedCommentThemes)
        {
            if (theme.OccurrenceCount < min)
            {
                continue;
            }

            string canonicalKey = "comment:" + theme.ThemeKey;

            if (buckets.ContainsKey(canonicalKey))
            {
                continue;
            }

            buckets[canonicalKey] = new ThemeAccumulator
            {
                CanonicalKey = canonicalKey,
                GroupingExplanation =
                    "Repeated pilot feedback notes grouped by the first "
                    + ProductLearningSignalAggregations.CommentThemePrefixLength
                    + " characters of trimmed CommentShort (identical to 58R comment themes; not semantic NLP).",
                Name = "Repeated feedback note (" + theme.OccurrenceCount + "×)",
                Description =
                    "ThemeKey=" + Truncate(theme.ThemeKey, 240) + "; sampleComment="
                    + Truncate(theme.SampleCommentShort, 400) + ".",
                EvidenceCount = theme.OccurrenceCount,
                AffectedArtifactTypes = new[] { "Feedback comments" },
                FirstSeenUtc = theme.FirstSeenUtc,
                LastSeenUtc = theme.LastSeenUtc,
            };
        }
    }

    private static void AddTagThemes(
        List<ProductLearningPilotSignalRecord> orderedSignals,
        ImprovementThemeExtractionOptions options,
        Dictionary<string, ThemeAccumulator> buckets)
    {
        int minTag = options.MinTagOccurrences < 1 ? 1 : options.MinTagOccurrences;

        Dictionary<string, List<(string Original, ProductLearningPilotSignalRecord Row)>> groups = new(StringComparer.Ordinal);

        foreach (ProductLearningPilotSignalRecord row in orderedSignals)
        {
            IReadOnlyList<string> tokens = ImprovementThemeDetailJsonAnnotations.ReadAnnotationTokens(row.DetailJson);

            foreach (string token in tokens)
            {
                string normalized = ImprovementThemeDetailJsonAnnotations.NormalizeAnnotationToken(token);

                if (normalized.Length == 0)
                {
                    continue;
                }

                if (!groups.TryGetValue(normalized, out List<(string, ProductLearningPilotSignalRecord)>? list))
                {
                    list = new List<(string, ProductLearningPilotSignalRecord)>();
                    groups[normalized] = list;
                }

                list.Add((token.Trim(), row));
            }
        }

        foreach (KeyValuePair<string, List<(string Original, ProductLearningPilotSignalRecord Row)>> pair in groups)
        {
            List<(string Original, ProductLearningPilotSignalRecord Row)> rows = pair.Value;

            int distinctSignals = rows.Select(static x => x.Row.SignalId).Distinct().Count();

            if (distinctSignals < minTag)
            {
                continue;
            }

            string canonicalKey = "tag:" + pair.Key;

            if (buckets.ContainsKey(canonicalKey))
            {
                continue;
            }

            string displayToken = rows
                .Select(static x => x.Original)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static s => s, StringComparer.Ordinal)
                .First();

            DateTime first = rows.Min(static x => x.Row.RecordedUtc);
            DateTime last = rows.Max(static x => x.Row.RecordedUtc);

            HashSet<string> facets = new(StringComparer.Ordinal);

            foreach ((string _, ProductLearningPilotSignalRecord row) in rows)
            {
                string facet = string.IsNullOrWhiteSpace(row.ArtifactHint) ? row.SubjectType : row.ArtifactHint.Trim();

                if (!string.IsNullOrWhiteSpace(facet))
                {
                    facets.Add(facet);
                }
            }

            buckets[canonicalKey] = new ThemeAccumulator
            {
                CanonicalKey = canonicalKey,
                GroupingExplanation =
                    "Pilot DetailJson object with string array (or string) properties tags, annotations, tag, or annotation; "
                    + "tokens matched case-insensitively after trim.",
                Name = "Tag / annotation: " + Truncate(displayToken, 200),
                Description =
                    "Normalized token key=" + pair.Key + "; matchingSignalRows=" + rows.Count + "; distinctSignals="
                    + distinctSignals + ".",
                EvidenceCount = distinctSignals,
                AffectedArtifactTypes = facets.Count == 0 ? Array.Empty<string>() : facets.OrderBy(static s => s, StringComparer.Ordinal).ToArray(),
                FirstSeenUtc = first,
                LastSeenUtc = last,
            };
        }
    }

    private static List<ImprovementThemeWithEvidence> Materialize(
        ProductLearningScope scope,
        Dictionary<string, ThemeAccumulator> buckets,
        List<ProductLearningPilotSignalRecord> orderedSignals,
        ImprovementThemeExtractionOptions options)
    {
        List<ImprovementThemeWithEvidence> list = new(buckets.Count);

        foreach (KeyValuePair<string, ThemeAccumulator> pair in buckets.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            ThemeAccumulator acc = pair.Value;
            Guid themeId = ImprovementThemeExtractionDeterministicIds.ThemeId(scope, acc.CanonicalKey);

            ImprovementTheme theme = new()
            {
                ThemeId = themeId,
                Name = acc.Name,
                Description = acc.Description,
                EvidenceCount = acc.EvidenceCount,
                AffectedArtifactTypes = acc.AffectedArtifactTypes,
                FirstSeenUtc = acc.FirstSeenUtc,
                LastSeenUtc = acc.LastSeenUtc,
            };

            IReadOnlyList<ImprovementThemeEvidence> examples = BuildExampleEvidence(
                themeId,
                acc.CanonicalKey,
                orderedSignals,
                options);

            list.Add(
                new ImprovementThemeWithEvidence
                {
                    Theme = theme,
                    ExampleEvidence = examples,
                    CanonicalKey = acc.CanonicalKey,
                    GroupingExplanation = acc.GroupingExplanation,
                });
        }

        return list;
    }

    private static void ApplyTriageSignalHints(
        List<ImprovementThemeWithEvidence> results,
        IReadOnlyList<TriageQueueItem>? triageQueue,
        List<ProductLearningPilotSignalRecord> orderedSignals,
        ImprovementThemeExtractionOptions options)
    {
        if (triageQueue is null || triageQueue.Count == 0)
        {
            return;
        }

        Dictionary<Guid, ProductLearningPilotSignalRecord> byId = orderedSignals.ToDictionary(static r => r.SignalId);

        int cap = options.MaxExampleEvidencePerTheme < 1 ? 1 : Math.Min(options.MaxExampleEvidencePerTheme, 50);

        for (int t = 0; t < triageQueue.Count; t++)
        {
            TriageQueueItem item = triageQueue[t];

            if (item.RelatedSignalId is null || item.RelatedSignalId.Value == Guid.Empty)
            {
                continue;
            }

            Guid signalId = item.RelatedSignalId.Value;

            if (!byId.TryGetValue(signalId, out ProductLearningPilotSignalRecord? row))
            {
                continue;
            }

            for (int i = 0; i < results.Count; i++)
            {
                ImprovementThemeWithEvidence theme = results[i];

                if (!SignalMatchesCanonicalKey(row, theme.CanonicalKey))
                {
                    continue;
                }

                if (theme.ExampleEvidence.Count >= cap)
                {
                    continue;
                }

                if (theme.ExampleEvidence.Any(e => e.SignalId == signalId))
                {
                    break;
                }

                Guid evidenceId = ImprovementThemeExtractionDeterministicIds.EvidenceId(
                    theme.Theme.ThemeId,
                    theme.CanonicalKey + "|triage|" + signalId.ToString("N"),
                    0);

                ImprovementThemeEvidence extra = new()
                {
                    EvidenceId = evidenceId,
                    ThemeId = theme.Theme.ThemeId,
                    ArchitectureRunId = row.ArchitectureRunId,
                    PilotArtifactHint = row.ArtifactHint,
                    SignalId = row.SignalId,
                };

                List<ImprovementThemeEvidence> merged = theme.ExampleEvidence.Concat(new[] { extra }).ToList();

                results[i] = new ImprovementThemeWithEvidence
                {
                    Theme = theme.Theme,
                    ExampleEvidence = merged,
                    CanonicalKey = theme.CanonicalKey,
                    GroupingExplanation = theme.GroupingExplanation
                        + " Triage queue referenced this signal as RelatedSignalId.",
                };

                break;
            }
        }
    }

    private static IReadOnlyList<ImprovementThemeEvidence> BuildExampleEvidence(
        Guid themeId,
        string canonicalKey,
        List<ProductLearningPilotSignalRecord> orderedSignals,
        ImprovementThemeExtractionOptions options)
    {
        int cap = options.MaxExampleEvidencePerTheme < 1 ? 1 : Math.Min(options.MaxExampleEvidencePerTheme, 50);

        List<ImprovementThemeEvidence> list = new(cap);

        foreach (ProductLearningPilotSignalRecord row in orderedSignals)
        {
            if (!SignalMatchesCanonicalKey(row, canonicalKey))
            {
                continue;
            }

            Guid evidenceId = ImprovementThemeExtractionDeterministicIds.EvidenceId(
                themeId,
                canonicalKey + "|" + row.SignalId.ToString("N"),
                0);

            list.Add(
                new ImprovementThemeEvidence
                {
                    EvidenceId = evidenceId,
                    ThemeId = themeId,
                    ArchitectureRunId = row.ArchitectureRunId,
                    PilotArtifactHint = row.ArtifactHint,
                    SignalId = row.SignalId,
                });

            if (list.Count >= cap)
            {
                break;
            }
        }

        return list;
    }

    private static bool SignalMatchesCanonicalKey(ProductLearningPilotSignalRecord row, string canonicalKey)
    {
        if (canonicalKey.StartsWith("rollup:", StringComparison.Ordinal))
        {
            string aggregateKey = canonicalKey["rollup:".Length..];

            string built = ProductLearningSignalAggregations.BuildAggregateKey(
                row.PatternKey,
                row.SubjectType,
                row.ArtifactHint);

            return string.Equals(built, aggregateKey, StringComparison.Ordinal);
        }

        if (canonicalKey.StartsWith("trend:", StringComparison.Ordinal))
        {
            string trendKey = canonicalKey["trend:".Length..];

            string built = ProductLearningSignalAggregations.BuildTrendKey(row.SubjectType, row.ArtifactHint);

            return string.Equals(built, trendKey, StringComparison.Ordinal);
        }

        if (canonicalKey.StartsWith("comment:", StringComparison.Ordinal))
        {
            string commentKey = canonicalKey["comment:".Length..];

            string? normalized = ProductLearningSignalAggregations.NormalizeCommentThemeKey(row.CommentShort);

            return normalized is not null &&
                   string.Equals(normalized, commentKey, StringComparison.Ordinal);
        }

        if (canonicalKey.StartsWith("tag:", StringComparison.Ordinal))
        {
            string tagKey = canonicalKey["tag:".Length..];

            IReadOnlyList<string> tokens = ImprovementThemeDetailJsonAnnotations.ReadAnnotationTokens(row.DetailJson);

            foreach (string token in tokens)
            {
                string n = ImprovementThemeDetailJsonAnnotations.NormalizeAnnotationToken(token);

                if (string.Equals(n, tagKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    private static bool PassesAggregateOutcomeGate(FeedbackAggregate aggregate, ImprovementThemeExtractionOptions options)
    {
        int minSignals = options.MinSignalsPerAggregateTheme < 1 ? 1 : options.MinSignalsPerAggregateTheme;

        if (aggregate.TotalSignalCount < minSignals)
        {
            return false;
        }

        int minBad = options.MinRejectedOrFollowUpForOutcomePattern < 0 ? 0 : options.MinRejectedOrFollowUpForOutcomePattern;
        int minRev = options.MinRevisedForRevisionPattern < 0 ? 0 : options.MinRevisedForRevisionPattern;

        int rejectOrFollowUp = aggregate.RejectedCount + aggregate.NeedsFollowUpCount;

        if (rejectOrFollowUp >= minBad)
        {
            return true;
        }

        if (aggregate.RevisedCount >= minRev)
        {
            return true;
        }

        return false;
    }

    private static bool PassesTrendGate(ArtifactOutcomeTrend trend, ImprovementThemeExtractionOptions options)
    {
        int minSignals = options.MinSignalsPerArtifactTrend < 1 ? 1 : options.MinSignalsPerArtifactTrend;
        int minNeg = options.MinNegativeOutcomesOnArtifactTrend < 0 ? 0 : options.MinNegativeOutcomesOnArtifactTrend;

        int total = TotalTrendSignals(trend);
        int negative = trend.RejectionCount + trend.RevisionCount + trend.NeedsFollowUpCount;

        return total >= minSignals && negative >= minNeg;
    }

    private static int TotalTrendSignals(ArtifactOutcomeTrend trend) =>
        trend.AcceptedOrTrustedCount + trend.RejectionCount + trend.RevisionCount + trend.NeedsFollowUpCount;

    private static string BuildRollupName(FeedbackAggregate aggregate)
    {
        if (!string.IsNullOrWhiteSpace(aggregate.PatternKey))
        {
            return "Pattern: " + Truncate(aggregate.PatternKey.Trim(), 200);
        }

        return "Workflow: " + Truncate(aggregate.SubjectTypeOrWorkflowArea, 200);
    }

    private static string BuildRollupDescription(FeedbackAggregate aggregate)
    {
        return
            $"AggregateKey={aggregate.AggregateKey}; totalSignals={aggregate.TotalSignalCount}; "
            + $"rejected={aggregate.RejectedCount}; needsFollowUp={aggregate.NeedsFollowUpCount}; revised={aggregate.RevisedCount}; "
            + $"trusted={aggregate.TrustedCount}; distinctRuns={aggregate.DistinctRunCount}.";
    }

    private static IReadOnlyList<string> CollectAffectedFromAggregate(FeedbackAggregate aggregate)
    {
        List<string> list = new(2);

        if (!string.IsNullOrWhiteSpace(aggregate.SubjectTypeOrWorkflowArea))
        {
            list.Add(aggregate.SubjectTypeOrWorkflowArea.Trim());
        }

        if (!string.IsNullOrWhiteSpace(aggregate.PatternKey))
        {
            list.Add("pattern:" + aggregate.PatternKey.Trim());
        }

        return list.Distinct(StringComparer.Ordinal).OrderBy(static s => s, StringComparer.Ordinal).ToArray();
    }

    private static string Truncate(string value, int maxChars)
    {
        if (value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars];
    }

    private sealed class ThemeAccumulator
    {
        public required string CanonicalKey { get; init; }

        public required string GroupingExplanation { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required int EvidenceCount { get; init; }

        public required IReadOnlyList<string> AffectedArtifactTypes { get; init; }

        public required DateTime FirstSeenUtc { get; init; }

        public required DateTime LastSeenUtc { get; init; }
    }
}
