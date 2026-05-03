using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentEvalRollupCommandTests
{
    [Fact]
    public async Task Rollup_from_json_writes_markdown_summary()
    {
        AgentOutputEvaluationSummary summary = new()
        {
            RunId = "abc123",
            EvaluatedAtUtc = new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc),
            TracesSkippedCount = 1,
            Scores =
            [
                new AgentOutputEvaluationScore
                {
                    TraceId = "t1",
                    AgentType = AgentType.Topology,
                    StructuralCompletenessRatio = 0.9,
                    IsJsonParseFailure = false,
                    Semantic = new AgentOutputSemanticScore
                    {
                        TraceId = "t1",
                        AgentType = AgentType.Topology,
                        OverallSemanticScore = 0.8,
                    },
                },
                new AgentOutputEvaluationScore
                {
                    TraceId = "t2",
                    AgentType = AgentType.Cost,
                    StructuralCompletenessRatio = 0.0,
                    IsJsonParseFailure = true,
                },
            ],
            AverageStructuralCompletenessRatio = 0.45,
            AverageSemanticScore = 0.8,
        };

        string path = Path.Combine(Path.GetTempPath(), $"agent-eval-rollup-test-{Guid.NewGuid():N}.json");
        JsonSerializerOptions writeOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(summary, writeOpts));

        try
        {
            StringWriter @out = new();
            StringWriter err = new();
            TextWriter prevOut = Console.Out;
            TextWriter prevErr = Console.Error;
            Console.SetOut(@out);
            Console.SetError(err);
            try
            {
                int code = await Program.RunAsync(["agent-eval", "rollup", "--from-json", path]);

                code.Should().Be(CliExitCode.Success);
                err.ToString().Should().BeEmpty();
                string text = @out.ToString();
                text.Should().Contain("abc123");
                text.Should().Contain("Execution mode caveat");
                text.Should().Contain("Topology");
                text.Should().Contain("parse failures inside scores: `1`");
            }
            finally
            {
                Console.SetOut(prevOut);
                Console.SetError(prevErr);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
