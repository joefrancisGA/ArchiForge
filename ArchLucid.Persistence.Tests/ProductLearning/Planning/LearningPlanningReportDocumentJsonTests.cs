using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

/// <summary>59R export document JSON shape (stable property names for clients).</summary>
[Trait("ChangeSet", "59R")]
public sealed class LearningPlanningReportDocumentJsonTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    [Fact]
    public void Serialize_round_trips_with_expected_numeric_fields()
    {
        DateTime generated = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        Guid planId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid themeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid signalId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        LearningPlanningReportDocument original = new()
        {
            GeneratedUtc = generated,
            Summary = new LearningPlanningReportSummaryBlock
            {
                ThemeCount = 1,
                PlanCount = 1,
                TotalThemeEvidenceSignals = 5,
                TotalLinkedSignalsAcrossPlans = 2,
                MaxPlanPriorityScore = 99
            },
            Themes =
            [
                new LearningPlanningReportThemeEntry
                {
                    ThemeId = themeId,
                    ThemeKey = "k",
                    Title = "T",
                    Summary = "s",
                    SeverityBand = "high",
                    EvidenceSignalCount = 5,
                    DistinctRunCount = 2,
                    Status = "open"
                }
            ],
            Plans =
            [
                new LearningPlanningReportPlanEntry
                {
                    PlanId = planId,
                    ThemeId = themeId,
                    ThemeTitle = "T",
                    Title = "P",
                    Summary = "ps",
                    PriorityScore = 99,
                    Status = "open",
                    CreatedUtc = generated,
                    ActionStepCount = 3,
                    Evidence = new LearningPlanningReportPlanEvidenceBlock
                    {
                        LinkedSignalCount = 2,
                        LinkedArtifactCount = 0,
                        LinkedArchitectureRunCount = 1,
                        Signals = [new LearningPlanningReportSignalRef { SignalId = signalId }],
                        Artifacts = [],
                        ArchitectureRunIds = ["run-1"]
                    }
                }
            ]
        };

        string json = JsonSerializer.Serialize(original, Options);
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("summary").GetProperty("themeCount").GetInt32());
        Assert.Equal(99, root.GetProperty("summary").GetProperty("maxPlanPriorityScore").GetInt32());
        Assert.Equal(1, root.GetProperty("themes").GetArrayLength());
        Assert.Equal(themeId.ToString("D"), root.GetProperty("themes")[0].GetProperty("themeId").GetString());

        JsonElement plan = root.GetProperty("plans")[0];
        Assert.Equal(99, plan.GetProperty("priorityScore").GetInt32());
        Assert.Equal(2, plan.GetProperty("evidence").GetProperty("linkedSignalCount").GetInt32());
        Assert.Equal(signalId.ToString("D"),
            plan.GetProperty("evidence").GetProperty("signals")[0].GetProperty("signalId").GetString());

        LearningPlanningReportDocument?
            back = JsonSerializer.Deserialize<LearningPlanningReportDocument>(json, Options);

        Assert.NotNull(back);
        Assert.Equal(original.Summary.TotalLinkedSignalsAcrossPlans, back.Summary.TotalLinkedSignalsAcrossPlans);
        Assert.Single(back.Plans);
        Assert.Equal(original.Plans[0].Evidence.LinkedSignalCount, back.Plans[0].Evidence.LinkedSignalCount);
    }
}
