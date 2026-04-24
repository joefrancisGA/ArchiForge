using ArchLucid.Api.Mapping;
using ArchLucid.Application.Analysis;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Replay Comparison Result Headers.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReplayComparisonResultHeadersTests
{
    [Fact]
    public void ApplyFull_writes_expected_core_and_optional_headers()
    {
        DefaultHttpContext context = new();
        ReplayComparisonResult result = new()
        {
            ComparisonRecordId = "cmp-1",
            ComparisonType = "end-to-end-replay",
            ReplayMode = "verify",
            VerificationPassed = false,
            VerificationMessage = "drift detected",
            LeftRunId = "run-left",
            RightRunId = "run-right",
            LeftExportRecordId = "exp-left",
            RightExportRecordId = "exp-right",
            CreatedUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            FormatProfile = "detailed",
            PersistedReplayRecordId = "cmp-new"
        };

        ReplayComparisonResultHeaders.ApplyFull(context.Response, result);

        context.Response.Headers["X-ArchLucid-ComparisonRecordId"].ToString().Should().Be("cmp-1");
        context.Response.Headers["X-ArchLucid-ComparisonType"].ToString().Should().Be("end-to-end-replay");
        context.Response.Headers["X-ArchLucid-ReplayMode"].ToString().Should().Be("verify");
        context.Response.Headers["X-ArchLucid-VerificationPassed"].ToString().Should().Be("False");
        context.Response.Headers["X-ArchLucid-VerificationMessage"].ToString().Should().Be("drift detected");
        context.Response.Headers["X-ArchLucid-LeftRunId"].ToString().Should().Be("run-left");
        context.Response.Headers["X-ArchLucid-RightRunId"].ToString().Should().Be("run-right");
        context.Response.Headers["X-ArchLucid-LeftExportRecordId"].ToString().Should().Be("exp-left");
        context.Response.Headers["X-ArchLucid-RightExportRecordId"].ToString().Should().Be("exp-right");
        context.Response.Headers["X-ArchLucid-CreatedUtc"].ToString().Should().Be("2026-01-02T03:04:05.0000000Z");
        context.Response.Headers["X-ArchLucid-Format-Profile"].ToString().Should().Be("detailed");
        context.Response.Headers["X-ArchLucid-PersistedReplayRecordId"].ToString().Should().Be("cmp-new");
    }

    [Fact]
    public void ApplyMetadata_writes_only_metadata_subset_headers()
    {
        DefaultHttpContext context = new();
        ReplayComparisonResult result = new()
        {
            ComparisonRecordId = "cmp-2",
            ComparisonType = "export-record-diff",
            ReplayMode = "artifact",
            VerificationPassed = true,
            LeftRunId = "run-left"
        };

        ReplayComparisonResultHeaders.ApplyMetadata(context.Response, result);

        context.Response.Headers.ContainsKey("X-ArchLucid-LeftRunId").Should().BeTrue();
        context.Response.Headers.ContainsKey("X-ArchLucid-ComparisonRecordId").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-ArchLucid-ReplayMode").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-ArchLucid-VerificationPassed").Should().BeFalse();
    }
}
