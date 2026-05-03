using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.Findings;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class FindingTraceConfidenceMapperTests
{
    [Fact]
    public void FromSnapshot_includes_rule_id_and_evidence_ref_count_from_structured_evidence()
    {
        FindingsSnapshot snapshot = new()
        {
            Findings =
            [
                new Finding
                {
                    FindingId = "f1",
                    FindingType = "t",
                    Category = "c",
                    EngineType = "e",
                    Title = "title",
                    Rationale = "r",
                    EvaluationConfidenceScore = 91,
                    ConfidenceLevel = FindingConfidenceLevel.High,
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["n1", "n2"],
                        RulesApplied = ["rule-z"],
                    },
                },
            ],
        };

        List<FindingTraceConfidenceDto> rows = FindingTraceConfidenceMapper.FromSnapshot(snapshot);

        rows.Should().ContainSingle();
        rows[0].FindingId.Should().Be("f1");
        rows[0].RuleId.Should().Be("rule-z");
        rows[0].EvidenceRefCount.Should().Be(2);
        rows[0].EvaluationConfidenceScore.Should().Be(91);
        rows[0].ConfidenceLevel.Should().Be(FindingConfidenceLevel.High);
    }
}
