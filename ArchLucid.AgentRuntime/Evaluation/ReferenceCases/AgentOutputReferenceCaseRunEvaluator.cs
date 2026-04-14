using System.Diagnostics;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Evaluation.ReferenceCases;

/// <summary>Scores traces against JSON reference cases (metrics + optional SQL persistence).</summary>
public sealed class AgentOutputReferenceCaseRunEvaluator(
    IOptionsMonitor<AgentExecutionReferenceEvaluationOptions> options,
    IAgentOutputReferenceCaseCatalog catalog,
    IAgentOutputEvaluator structuralEvaluator,
    IAgentOutputSemanticEvaluator semanticEvaluator,
    IAgentOutputEvaluationResultRepository resultRepository,
    ILogger<AgentOutputReferenceCaseRunEvaluator> logger)
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    /// <summary>Evaluates one trace against all cases matching its <see cref="AgentExecutionTrace.AgentType"/>.</summary>
    public async Task EvaluateTraceAsync(
        AgentExecutionTrace trace,
        string runId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentException.ThrowIfNullOrEmpty(runId);

        if (!options.CurrentValue.Enabled)
        {
            return;
        }

        IReadOnlyList<AgentOutputReferenceCaseDefinition> cases = catalog.Cases;

        if (cases.Count == 0)
        {
            return;
        }

        if (!trace.ParseSucceeded || string.IsNullOrEmpty(trace.ParsedResultJson))
        {
            return;
        }

        string agentLabel = trace.AgentType.ToString();

        foreach (AgentOutputReferenceCaseDefinition caseDef in cases)
        {
            if (caseDef.AgentType != trace.AgentType)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            AgentOutputEvaluationScore structural = structuralEvaluator.Evaluate(
                trace.TraceId,
                trace.ParsedResultJson,
                trace.AgentType);

            if (structural.IsJsonParseFailure)
            {
                continue;
            }

            AgentOutputSemanticScore semantic = semanticEvaluator.Evaluate(
                trace.TraceId,
                trace.ParsedResultJson,
                trace.AgentType);

            bool pass = EvaluateCaseRules(caseDef, trace.ParsedResultJson, structural, semantic, out string? failureReason);

            double overall = (structural.StructuralCompletenessRatio + semantic.OverallSemanticScore) / 2.0;

            TagList metricTags = new()
            {
                { "case_id", caseDef.CaseId },
                { "agent_type", agentLabel },
                { "outcome", pass ? "pass" : "fail" },
            };

            ArchLucidInstrumentation.AgentOutputReferenceCaseEvaluationsTotal.Add(1, metricTags);
            ArchLucidInstrumentation.AgentOutputReferenceCaseScoreRatio.Record(
                overall,
                new TagList { { "case_id", caseDef.CaseId }, { "agent_type", agentLabel } });

            if (!pass && failureReason is not null)
            {
                logger.LogDebug(
                    "Reference case {CaseId} failed for run {RunId} trace {TraceId}: {Reason}",
                    LogSanitizer.Sanitize(caseDef.CaseId),
                    LogSanitizer.Sanitize(runId),
                    LogSanitizer.Sanitize(trace.TraceId),
                    LogSanitizer.Sanitize(failureReason));
            }

            string? missingKeysJson = structural.MissingKeys.Count > 0
                ? JsonSerializer.Serialize(structural.MissingKeys, WebJson)
                : null;

            AgentOutputEvaluationResultInsert row = new()
            {
                RunId = runId,
                TraceId = trace.TraceId,
                CaseId = caseDef.CaseId,
                AgentType = trace.AgentType,
                OverallScore = overall,
                StructuralMatch = structural.StructuralCompletenessRatio,
                SemanticMatch = semantic.OverallSemanticScore,
                MissingKeysJson = missingKeysJson,
                CreatedUtc = DateTime.UtcNow,
            };

            await resultRepository.AppendAsync(row, cancellationToken);
        }
    }

    private static bool EvaluateCaseRules(
        AgentOutputReferenceCaseDefinition caseDef,
        string parsedResultJson,
        AgentOutputEvaluationScore structural,
        AgentOutputSemanticScore semantic,
        out string? failureReason)
    {
        failureReason = null;

        if (caseDef.MinimumStructuralCompleteness > 0
            && structural.StructuralCompletenessRatio + 1e-9 < caseDef.MinimumStructuralCompleteness)
        {
            failureReason =
                $"structural {structural.StructuralCompletenessRatio:F3} < min {caseDef.MinimumStructuralCompleteness:F3}";

            return false;
        }

        if (caseDef.MinimumSemanticScore > 0
            && semantic.OverallSemanticScore + 1e-9 < caseDef.MinimumSemanticScore)
        {
            failureReason =
                $"semantic {semantic.OverallSemanticScore:F3} < min {caseDef.MinimumSemanticScore:F3}";

            return false;
        }

        if (caseDef.RequiredJsonKeys.Count > 0)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(parsedResultJson);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    failureReason = "root is not a JSON object (required keys)";

                    return false;
                }

                HashSet<string> names = new(StringComparer.Ordinal);

                foreach (JsonProperty p in doc.RootElement.EnumerateObject())
                {
                    names.Add(p.Name);
                }

                foreach (string key in caseDef.RequiredJsonKeys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    if (!names.Contains(key.Trim()))
                    {
                        failureReason = $"missing key '{key.Trim()}'";

                        return false;
                    }
                }
            }
            catch (JsonException)
            {
                failureReason = "JSON parse failed for required keys";

                return false;
            }
        }

        bool needsAgentResult = caseDef.MinimumFindingCount > 0
                                || caseDef.ExpectedFindingCategories.Any(static c => !string.IsNullOrWhiteSpace(c));

        if (!needsAgentResult)
        {
            return true;
        }

        AgentResult? actual = TryDeserializeAgentResult(parsedResultJson);

        if (actual is null)
        {
            failureReason = "could not deserialize AgentResult for finding rules";

            return false;
        }

        if (caseDef.MinimumFindingCount > 0 && actual.Findings.Count < caseDef.MinimumFindingCount)
        {
            failureReason =
                $"findings {actual.Findings.Count} < min {caseDef.MinimumFindingCount}";

            return false;
        }

        HashSet<string> findingCategories = actual.Findings
            .Select(f => f.Category.Trim())
            .Where(s => s.Length > 0)
            .Select(s => s.ToUpperInvariant())
            .ToHashSet();

        foreach (string cat in caseDef.ExpectedFindingCategories)
        {
            if (string.IsNullOrWhiteSpace(cat))
            {
                continue;
            }

            if (!findingCategories.Contains(cat.Trim().ToUpperInvariant()))
            {
                failureReason = $"missing finding category '{cat.Trim()}'";

                return false;
            }
        }

        return true;
    }

    private static AgentResult? TryDeserializeAgentResult(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AgentResult>(json, WebJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
