using ArchiForge.Api.Mapping;
using ArchiForge.Api.Models;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ReplayComparisonRequestMapperTests
{
    [Fact]
    public void ToApplicationForReplayEndpoint_prefers_query_format_when_body_is_blank()
    {
        ReplayComparisonRequest request = new ReplayComparisonRequest
        {
            Format = "",
            ReplayMode = "verify",
            Profile = "detailed",
            PersistReplay = true
        };

        Application.Analysis.ReplayComparisonRequest mapped = ReplayComparisonRequestMapper.ToApplicationForReplayEndpoint("cmp-1", request, "html");

        mapped.ComparisonRecordId.Should().Be("cmp-1");
        mapped.Format.Should().Be("html");
        mapped.ReplayMode.Should().Be("verify");
        mapped.Profile.Should().Be("detailed");
        mapped.PersistReplay.Should().BeTrue();
    }

    [Fact]
    public void ToApplicationForReplayEndpoint_keeps_body_format_when_present()
    {
        ReplayComparisonRequest request = new ReplayComparisonRequest { Format = "docx" };

        Application.Analysis.ReplayComparisonRequest mapped = ReplayComparisonRequestMapper.ToApplicationForReplayEndpoint("cmp-2", request, "html");

        mapped.Format.Should().Be("docx");
    }

    [Fact]
    public void ForSummaryMarkdown_returns_expected_defaults()
    {
        Application.Analysis.ReplayComparisonRequest mapped = ReplayComparisonRequestMapper.ForSummaryMarkdown("cmp-3");

        mapped.ComparisonRecordId.Should().Be("cmp-3");
        mapped.Format.Should().Be("markdown");
        mapped.ReplayMode.Should().Be("artifact");
        mapped.PersistReplay.Should().BeFalse();
    }

    [Fact]
    public void ToApplicationForBatchEntry_maps_all_fields()
    {
        Application.Analysis.ReplayComparisonRequest mapped = ReplayComparisonRequestMapper.ToApplicationForBatchEntry(
            "cmp-4",
            "json",
            "regenerate",
            "executive",
            persistReplay: true);

        mapped.ComparisonRecordId.Should().Be("cmp-4");
        mapped.Format.Should().Be("json");
        mapped.ReplayMode.Should().Be("regenerate");
        mapped.Profile.Should().Be("executive");
        mapped.PersistReplay.Should().BeTrue();
    }
}
