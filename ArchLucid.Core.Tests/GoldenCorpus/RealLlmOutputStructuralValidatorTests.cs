using System.Text.Json;

using ArchLucid.Contracts.Common;

using ArchLucid.Core.GoldenCorpus;

using FluentAssertions;

namespace ArchLucid.Core.Tests.GoldenCorpus;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RealLlmOutputStructuralValidatorTests
{
    [Theory]
    [InlineData(AgentType.Topology)]
    [InlineData(AgentType.Cost)]
    [InlineData(AgentType.Compliance)]
    [InlineData(AgentType.Critic)]
    public void ValidateAgentResultStructure_accepts_minimal_valid_json_for_each_type(AgentType type)
    {
        string name = type.ToString();
        string json = MinimalValidResultJson(name, 1, true);

        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure(name, json);

        r.IsValid.Should().BeTrue();
        r.Checks.Should().NotBeEmpty();
        r.Checks.Should().OnlyContain(c => c.Passed);
    }

    [Theory]
    [InlineData(AgentType.Topology, "1")]
    [InlineData(AgentType.Cost, "2")]
    [InlineData(AgentType.Compliance, "3")]
    [InlineData(AgentType.Critic, "4")]
    public void ValidateAgentResultStructure_accepts_enum_numeric_agentType_in_json(AgentType type, string numeric)
    {
        string json = MinimalValidResultJson(numeric, 1, true);

        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure(type.ToString(), json);

        r.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(AgentType.Topology, "2")]
    [InlineData(AgentType.Cost, "999")]
    public void ValidateAgentResultStructure_rejects_param_agentType_mismatch(AgentType expected, string wrongJsonType)
    {
        string json = MinimalValidResultJson(wrongJsonType, 1, true);

        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure(expected.ToString(), json);

        r.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(AgentType.Topology)]
    [InlineData(AgentType.Critic)]
    public void ValidateAgentResultStructure_rejects_invalid_json_syntax(AgentType type)
    {
        RealLlmStructuralValidationResult r =
            RealLlmOutputStructuralValidator.ValidateAgentResultStructure(type.ToString(), "not-json{");

        r.IsValid.Should().BeFalse();
        r.Checks.Should().Contain(c => c.Name == "jsonSyntax" && c.Passed == false);
    }

    [Fact]
    public void ValidateAgentResultStructure_rejects_empty_top_level()
    {
        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure("Topology", "[]");

        r.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(AgentType.Topology, "resultId")]
    [InlineData(AgentType.Cost, "findings")]
    [InlineData(AgentType.Compliance, "evidenceRefs")]
    public void ValidateAgentResultStructure_rejects_missing_base_key(AgentType t, string omit)
    {
        string full = MinimalValidResultJson(t.ToString(), 1, true);
        using JsonDocument doc = JsonDocument.Parse(full);
        Dictionary<string, JsonElement> o = new();

        foreach (JsonProperty p in doc.RootElement.EnumerateObject())
        {
            if (!string.Equals(p.Name, omit, StringComparison.Ordinal))
                o[p.Name] = p.Value.Clone();
        }

        string json = JsonSerializer.Serialize(o);
        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure(t.ToString(), json);

        r.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(AgentType.Topology)]
    [InlineData(AgentType.Critic)]
    public void ValidateAgentResultStructure_rejects_empty_findings(AgentType t)
    {
        string json = MinimalValidResultJson(t.ToString(), 0, true);
        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure(t.ToString(), json);

        r.IsValid.Should().BeFalse();
        r.Checks.Should().Contain(c => c.Name == "findingsNonEmpty" && c.Passed == false);
    }

    [Theory]
    [InlineData(AgentType.Topology)]
    [InlineData(AgentType.Compliance)]
    public void ValidateAgentResultStructure_rejects_missing_trace_object(AgentType t)
    {
        string json = MinimalValidResultJson(t.ToString(), 1, false);
        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure(t.ToString(), json);

        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentResultStructure_rejects_trace_list_field_wrong_type()
    {
        const string broken = """
            {
              "resultId": "r1",
              "taskId": "t1",
              "runId": "run1",
              "agentType": "Topology",
              "claims": [""],
              "evidenceRefs": [""],
              "confidence": 0.5,
              "createdUtc": "2026-01-01T00:00:00Z",
              "findings": [
                {
                  "findingId": "f1",
                  "trace": {
                    "sourceAgentExecutionTraceId": null,
                    "graphNodeIdsExamined": "not-an-array",
                    "rulesApplied": [],
                    "decisionsTaken": [],
                    "alternativePathsConsidered": [],
                    "notes": []
                  }
                }
              ]
            }
            """;

        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure("Topology", broken);

        r.IsValid.Should().BeFalse();
        r.Checks.Should().Contain(c => c.Name == "traceLists" && c.Passed == false);
    }

    [Fact]
    public void ValidateAgentResultStructure_rejects_empty_agentType_parameter()
    {
        RealLlmStructuralValidationResult r = RealLlmOutputStructuralValidator.ValidateAgentResultStructure("   ", "{}");

        r.IsValid.Should().BeFalse();
    }

    private static string MinimalValidResultJson(string agentTypeField, int findingCount, bool includeTrace)
    {
        List<Dictionary<string, object?>> findings = [];

        for (int i = 0; i < findingCount; i++)
        {
            Dictionary<string, object?> finding = new() { { "findingId", $"f{i.ToString(System.Globalization.CultureInfo.InvariantCulture)}" } };

            if (includeTrace)
            {
                finding["trace"] = new Dictionary<string, object?>
                {
                    ["sourceAgentExecutionTraceId"] = null,
                    ["graphNodeIdsExamined"] = new List<string>(),
                    ["rulesApplied"] = new List<string>(),
                    ["decisionsTaken"] = new List<string>(),
                    ["alternativePathsConsidered"] = new List<string>(),
                    ["notes"] = new List<string>()
                };
            }

            findings.Add(finding);
        }

        Dictionary<string, object?> root = new()
        {
            ["resultId"] = "r1",
            ["taskId"] = "t1",
            ["runId"] = "run1",
            ["agentType"] = agentTypeField,
            ["claims"] = new List<string> { "c" },
            ["evidenceRefs"] = new List<string> { "e" },
            ["confidence"] = 0.5,
            ["createdUtc"] = "2026-01-01T00:00:00Z",
            ["findings"] = findings
        };

        return JsonSerializer.Serialize(root);
    }
}
