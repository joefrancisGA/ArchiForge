using System.Text.Json;

using ArchLucid.Contracts.Agents;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>Shared quality-gate semantics for finding-confidence enrichment (matches <see cref="AgentOutputEvaluationRecorder" />).</summary>
internal static class AgentOutputFindingConfidenceSignals
{
    internal static bool ComputeQualityGateAccepted(
        AgentExecutionTrace trace,
        IAgentOutputEvaluator structuralEvaluator,
        IAgentOutputSemanticEvaluator semanticEvaluator,
        IAgentOutputQualityGate qualityGate)
    {
        if (!trace.ParseSucceeded || string.IsNullOrEmpty(trace.ParsedResultJson))
            return false;

        AgentOutputEvaluationScore score =
            structuralEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);

        if (score.IsJsonParseFailure)
            return false;

        AgentOutputSemanticScore semanticScore =
            semanticEvaluator.Evaluate(trace.TraceId, trace.ParsedResultJson, trace.AgentType);

        AgentOutputQualityGateOutcome gateOutcome = qualityGate.Evaluate(score, semanticScore);

        bool hasCitations = false;

        try
        {
            using JsonDocument doc = JsonDocument.Parse(trace.ParsedResultJson);

            if (doc.RootElement.TryGetProperty("citations", out JsonElement citationsElement) &&
                citationsElement.ValueKind == JsonValueKind.Array &&
                citationsElement.GetArrayLength() > 0)
            {
                hasCitations = true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        if (!hasCitations)
            gateOutcome = AgentOutputQualityGateOutcome.Rejected;

        return gateOutcome != AgentOutputQualityGateOutcome.Rejected;
    }
}
