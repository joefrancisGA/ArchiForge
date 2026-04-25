using System.Text.Json;

using ArchLucid.Contracts.Common;

namespace ArchLucid.Core.GoldenCorpus;

/// <summary>
///     Content-agnostic JSON checks for a single <see cref="ArchLucid.Contracts.Agents.AgentResult" /> document returned
///     from the API (camelCase). Does not assert on claim text, finding messages, or category values.
/// </summary>
public static class RealLlmOutputStructuralValidator
{
    private static readonly string[] BaseTopLevelKeys =
    [
        "resultId", "taskId", "runId", "agentType", "claims", "evidenceRefs", "confidence", "createdUtc", "findings"
    ];

    /// <summary>ExplainabilityTrace list fields; <c>sourceAgentExecutionTraceId</c> is optional and may be null/omitted.</summary>
    private static readonly string[] TraceListKeys =
    [
        "graphNodeIdsExamined", "rulesApplied", "decisionsTaken", "alternativePathsConsidered", "notes"
    ];

    /// <summary>
    ///     Validates that <paramref name="resultJson" /> is a well-formed <c>AgentResult</c> with a non-empty
    ///     <c>findings</c> array and a <c>trace</c> object on every finding (ExplainabilityTrace shape).
    /// </summary>
    /// <param name="agentType">Expected <c>agentType</c> (e.g. <c>Topology</c>); must match the JSON <c>agentType</c> field.</param>
    /// <param name="resultJson">Raw JSON (typically camelCase from the contract serializer).</param>
    public static RealLlmStructuralValidationResult ValidateAgentResultStructure(string agentType, string resultJson)
    {
        if (string.IsNullOrWhiteSpace(agentType))
            return Fail("agentTypeParameter", "The agentType parameter is required.");

        if (!TryResolveAgentType(agentType.Trim(), out AgentType expectedEnum, out string? typeParseError))
            return Fail("expectedAgentType", typeParseError ?? "Unknown agent type.");

        List<RealLlmStructuralCheckItem> checks = [];

        JsonDocument? doc = null;

        try
        {
            doc = JsonDocument.Parse(resultJson);
        }
        catch (JsonException ex)
        {
            checks.Add(new RealLlmStructuralCheckItem("jsonSyntax", false, ex.Message));

            return new RealLlmStructuralValidationResult(false, checks);
        }

        using (doc)
        {
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                checks.Add(
                    new RealLlmStructuralCheckItem("rootObject", false, "Root JSON value must be an object."));

                return new RealLlmStructuralValidationResult(false, checks);
            }

            foreach (string key in BaseTopLevelKeys)
            {
                if (!root.TryGetProperty(key, out _))
                {
                    checks.Add(
                        new RealLlmStructuralCheckItem(
                            "topLevelKeys",
                            false,
                            $"Missing required top-level property '{key}' on AgentResult JSON."));

                    return new RealLlmStructuralValidationResult(false, checks);
                }
            }

            if (!root.TryGetProperty("agentType", out JsonElement agentTypeEl))
            {
                checks.Add(
                    new RealLlmStructuralCheckItem("agentTypeField", false, "Property 'agentType' is missing."));

                return new RealLlmStructuralValidationResult(false, checks);
            }

            if (!JsonAgentTypeMatchesExpected(agentTypeEl, expectedEnum, out string? atMsg))
            {
                checks.Add(new RealLlmStructuralCheckItem("agentTypeMatch", false, atMsg ?? "agentType mismatch."));

                return new RealLlmStructuralValidationResult(false, checks);
            }

            checks.Add(
                new RealLlmStructuralCheckItem(
                    "topLevelKeys",
                    true,
                    "All required top-level properties for AgentResult are present."));

            checks.Add(
                new RealLlmStructuralCheckItem("agentTypeMatch", true, $"agentType is {expectedEnum} as required."));

            if (!root.TryGetProperty("findings", out JsonElement findings) || findings.ValueKind != JsonValueKind.Array
                || findings.GetArrayLength() == 0)
            {
                checks.Add(
                    new RealLlmStructuralCheckItem(
                        "findingsNonEmpty",
                        false,
                        "Property 'findings' must be a non-empty JSON array."));

                return new RealLlmStructuralValidationResult(false, checks);
            }

            checks.Add(
                new RealLlmStructuralCheckItem("findingsNonEmpty", true, "Findings array is non-empty."));

            int index = 0;

            foreach (JsonElement finding in findings.EnumerateArray())
            {
                if (finding.ValueKind != JsonValueKind.Object)
                {
                    checks.Add(
                        new RealLlmStructuralCheckItem(
                            "findingObject",
                            false,
                            $"findings[{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}] must be an object."));

                    return new RealLlmStructuralValidationResult(false, checks);
                }

                if (!finding.TryGetProperty("trace", out JsonElement trace) || trace.ValueKind != JsonValueKind.Object)
                {
                    checks.Add(
                        new RealLlmStructuralCheckItem(
                            "findingTrace",
                            false,
                            $"findings[{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}] must include an object 'trace' (ExplainabilityTrace)."));

                    return new RealLlmStructuralValidationResult(false, checks);
                }

                if (trace.TryGetProperty("sourceAgentExecutionTraceId", out JsonElement sid)
                    && sid.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                {
                    checks.Add(
                        new RealLlmStructuralCheckItem(
                            "traceSourceId",
                            false,
                            "Optional 'sourceAgentExecutionTraceId' must be a string or null when present."));

                    return new RealLlmStructuralValidationResult(false, checks);
                }

                foreach (string listKey in TraceListKeys)
                {
                    if (!trace.TryGetProperty(listKey, out JsonElement listEl) || listEl.ValueKind != JsonValueKind.Array)
                    {
                        checks.Add(
                            new RealLlmStructuralCheckItem(
                                "traceLists",
                                false,
                                $"ExplainabilityTrace must include array '{listKey}' (findings[{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}].trace)."));

                        return new RealLlmStructuralValidationResult(false, checks);
                    }
                }

                index++;
            }

            checks.Add(
                new RealLlmStructuralCheckItem(
                    "explainabilityTraceShape",
                    true,
                    "Each finding has a trace object with ExplainabilityTrace list fields."));

            return new RealLlmStructuralValidationResult(RealLlmStructuralValidationResult.AllPassed(checks), checks);
        }
    }

    private static RealLlmStructuralValidationResult Fail(string name, string message) =>
        new(
            false,
            [new RealLlmStructuralCheckItem(name, false, message)]);

    private static bool TryResolveAgentType(
        string input,
        out AgentType type,
        out string? error)
    {
        type = default;
        error = null;

        if (Enum.TryParse<AgentType>(input, true, out type) && Enum.IsDefined(type))
            return true;

        if (int.TryParse(input, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int n)
            && Enum.IsDefined(typeof(AgentType), n))
        {
            type = (AgentType)n;

            return true;
        }

        error = $"Parameter agentType '{input}' is not a valid AgentType name or integer.";

        return false;
    }

    private static bool JsonAgentTypeMatchesExpected(JsonElement agentTypeEl, AgentType expected, out string? message)
    {
        message = null;

        return agentTypeEl.ValueKind switch
        {
            JsonValueKind.String => EnumTryParseLenient(agentTypeEl.GetString(), expected, out message),
            JsonValueKind.Number => agentTypeEl.TryGetInt32(out int n) && Enum.IsDefined(typeof(AgentType), n) && (AgentType)n == expected
                ? true
                : SetMsg(out message, "agentType number does not match the expected type."),
            _ => SetFalse(out message, "agentType must be a string or number.")
        };
    }

    private static bool EnumTryParseLenient(string? text, AgentType expected, out string? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            message = "agentType string is empty.";

            return false;
        }

        if (Enum.TryParse<AgentType>(text, true, out AgentType t) && t == expected)
            return true;

        message = $"JSON agentType '{text}' does not match expected {expected}.";

        return false;
    }

    private static bool SetMsg(out string? m, string text)
    {
        m = text;

        return false;
    }

    private static bool SetFalse(out string? m, string text)
    {
        m = text;

        return false;
    }
}
