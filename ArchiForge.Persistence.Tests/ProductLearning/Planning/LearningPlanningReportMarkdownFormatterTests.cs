using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Persistence.ProductLearning.Planning;

namespace ArchiForge.Persistence.Tests.ProductLearning.Planning;

public sealed class LearningPlanningReportMarkdownFormatterTests
{
    [Fact]
    public void Format_emits_fixed_headings_and_stable_numeric_formatting()
    {
        DateTime generated = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid themeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid planId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid sigId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        LearningPlanningReportDocument doc = new()
        {
            GeneratedUtc = generated,
            Summary = new LearningPlanningReportSummaryBlock
            {
                ThemeCount = 1,
                PlanCount = 1,
                TotalThemeEvidenceSignals = 3,
                TotalLinkedSignalsAcrossPlans = 1,
                MaxPlanPriorityScore = 10,
            },
            Themes =
            [
                new LearningPlanningReportThemeEntry
                {
                    ThemeId = themeId,
                    ThemeKey = "k1",
                    Title = "Theme A",
                    Summary = "S",
                    SeverityBand = "High",
                    EvidenceSignalCount = 3,
                    DistinctRunCount = 2,
                    Status = "open",
                },
            ],
            Plans =
            [
                new LearningPlanningReportPlanEntry
                {
                    PlanId = planId,
                    ThemeId = themeId,
                    ThemeTitle = "Theme A",
                    Title = "Plan A",
                    Summary = "Do work",
                    PriorityScore = 10,
                    PriorityExplanation = "Because",
                    Status = "proposed",
                    CreatedUtc = generated,
                    ActionStepCount = 2,
                    Evidence = new LearningPlanningReportPlanEvidenceBlock
                    {
                        LinkedSignalCount = 1,
                        LinkedArtifactCount = 0,
                        LinkedArchitectureRunCount = 0,
                        Signals =
                        [
                            new LearningPlanningReportSignalRef
                            {
                                SignalId = sigId,
                                TriageStatusSnapshot = "open",
                            },
                        ],
                        Artifacts = [],
                        ArchitectureRunIds = [],
                    },
                },
            ],
        };

        string md = LearningPlanningReportMarkdownFormatter.Format(doc);

        Assert.Contains("# ArchiForge planning report (59R)", md, StringComparison.Ordinal);
        Assert.Contains("## Top improvement themes", md, StringComparison.Ordinal);
        Assert.Contains("### 1. Theme A", md, StringComparison.Ordinal);
        Assert.Contains("## Prioritized improvement plans", md, StringComparison.Ordinal);
        Assert.Contains("### 1. Plan A", md, StringComparison.Ordinal);
        Assert.Contains("- Priority score: 10", md, StringComparison.Ordinal);
        Assert.Contains("`aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`", md, StringComparison.Ordinal);
        Assert.EndsWith(Environment.NewLine, md, StringComparison.Ordinal);
    }
}
