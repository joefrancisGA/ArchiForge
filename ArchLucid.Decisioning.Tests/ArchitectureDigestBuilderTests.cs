using System.Text.Json;

using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Decisioning.Alerts;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Architecture Digest Builder.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchitectureDigestBuilderTests
{
    private readonly ArchitectureDigestBuilder _sut = new();

    /// <summary>Seed more recommendations than the digest top-N so truncation is exercised.</summary>
    private const int RecommendationSeedCountBeyondDigestTopN = 7;

    private static ImprovementPlan EmptyPlan() => new()
    {
        RunId = Guid.NewGuid(),
        GeneratedUtc = new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Build_NoRecommendations_UsesNoRecommendationsNote()
    {
        ImprovementPlan plan = EmptyPlan();

        ArchitectureDigest digest = _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, plan);

        digest.ContentMarkdown.Should().Contain("No significant recommendations were identified.");
    }

    [Fact]
    public void Build_NoAlerts_UsesNoAlertsNote()
    {
        ImprovementPlan plan = EmptyPlan();

        ArchitectureDigest digest = _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, plan);

        digest.ContentMarkdown.Should().Contain("No alerts were triggered.");
    }

    [Fact]
    public void Build_SevenRecommendations_TakesTopFiveByPriority()
    {
        ImprovementPlan plan = EmptyPlan();
        for (int i = 0; i < RecommendationSeedCountBeyondDigestTopN; i++)
        {
            plan.Recommendations.Add(new ImprovementRecommendation
            {
                Title = $"Item {i}",
                Category = "Security",
                Rationale = "r",
                SuggestedAction = "a",
                Urgency = "High",
                ExpectedImpact = "e",
                PriorityScore = i
            });
        }

        ArchitectureDigest digest = _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, plan);

        digest.ContentMarkdown.Should().Contain("### Item 6");
        digest.ContentMarkdown.Should().Contain("### Item 5");
        digest.ContentMarkdown.Should().NotContain("### Item 0");
        digest.ContentMarkdown.Should().NotContain("### Item 1");
    }

    [Fact]
    public void Build_WithAlerts_ListsEachAlertLine()
    {
        ImprovementPlan plan = EmptyPlan();
        List<AlertRecord> alerts =
        [
            new()
            {
                Severity = AlertSeverity.Warning,
                Title = "A1",
                Description = "D1",
                Category = "Test",
                TriggerValue = "t",
                DeduplicationKey = "k1"
            },
            new()
            {
                Severity = AlertSeverity.Info,
                Title = "A2",
                Description = "D2",
                Category = "Test",
                TriggerValue = "t",
                DeduplicationKey = "k2"
            }
        ];

        ArchitectureDigest digest = _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, plan, alerts);

        digest.ContentMarkdown.Should().Contain("[Warning] A1 — D1");
        digest.ContentMarkdown.Should().Contain("[Info] A2 — D2");
    }

    [Fact]
    public void Build_MetadataJson_CountsHighOrCriticalAlerts()
    {
        ImprovementPlan plan = EmptyPlan();
        List<AlertRecord> alerts =
        [
            new()
            {
                Severity = AlertSeverity.High,
                Title = "H",
                Description = "x",
                Category = "Test",
                TriggerValue = "t",
                DeduplicationKey = "h"
            },
            new()
            {
                Severity = AlertSeverity.Critical,
                Title = "C",
                Description = "x",
                Category = "Test",
                TriggerValue = "t",
                DeduplicationKey = "c"
            },
            new()
            {
                Severity = AlertSeverity.Info,
                Title = "I",
                Description = "x",
                Category = "Test",
                TriggerValue = "t",
                DeduplicationKey = "i"
            }
        ];

        ArchitectureDigest digest = _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, plan, alerts);

        using JsonDocument doc = JsonDocument.Parse(digest.MetadataJson);
        JsonElement root = doc.RootElement;
        root.GetProperty("evaluatedAlertCount").GetInt32().Should().Be(3);
        root.GetProperty("highOrCriticalAlertCount").GetInt32().Should().Be(2);
        root.GetProperty("recommendationCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public void Build_NullPlan_Throws()
    {
        Action act = () => _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_ComparedToRunId_PrintsLineWhenSet()
    {
        ImprovementPlan plan = EmptyPlan();
        Guid compared = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        ArchitectureDigest digest = _sut.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, compared, plan);

        digest.ContentMarkdown.Should().Contain("Compared to prior run:");
        digest.ContentMarkdown.Should().Contain(compared.ToString("N"));
    }
}
