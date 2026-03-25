using ArchiForge.Api.Mapping;
using ArchiForge.Application.Analysis;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ReplayComparisonResultHeadersTests
{
    [Fact]
    public void ApplyFull_writes_expected_core_and_optional_headers()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        ReplayComparisonResult result = new ReplayComparisonResult
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

        context.Response.Headers["X-ArchiForge-ComparisonRecordId"].ToString().Should().Be("cmp-1");
        context.Response.Headers["X-ArchiForge-ComparisonType"].ToString().Should().Be("end-to-end-replay");
        context.Response.Headers["X-ArchiForge-ReplayMode"].ToString().Should().Be("verify");
        context.Response.Headers["X-ArchiForge-VerificationPassed"].ToString().Should().Be("False");
        context.Response.Headers["X-ArchiForge-VerificationMessage"].ToString().Should().Be("drift detected");
        context.Response.Headers["X-ArchiForge-LeftRunId"].ToString().Should().Be("run-left");
        context.Response.Headers["X-ArchiForge-RightRunId"].ToString().Should().Be("run-right");
        context.Response.Headers["X-ArchiForge-LeftExportRecordId"].ToString().Should().Be("exp-left");
        context.Response.Headers["X-ArchiForge-RightExportRecordId"].ToString().Should().Be("exp-right");
        context.Response.Headers["X-ArchiForge-CreatedUtc"].ToString().Should().Be("2026-01-02T03:04:05.0000000Z");
        context.Response.Headers["X-ArchiForge-Format-Profile"].ToString().Should().Be("detailed");
        context.Response.Headers["X-ArchiForge-PersistedReplayRecordId"].ToString().Should().Be("cmp-new");
    }

    [Fact]
    public void ApplyMetadata_writes_only_metadata_subset_headers()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        ReplayComparisonResult result = new ReplayComparisonResult
        {
            ComparisonRecordId = "cmp-2",
            ComparisonType = "export-record-diff",
            ReplayMode = "artifact",
            VerificationPassed = true,
            LeftRunId = "run-left"
        };

        ReplayComparisonResultHeaders.ApplyMetadata(context.Response, result);

        context.Response.Headers.ContainsKey("X-ArchiForge-LeftRunId").Should().BeTrue();
        context.Response.Headers.ContainsKey("X-ArchiForge-ComparisonRecordId").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-ArchiForge-ReplayMode").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-ArchiForge-VerificationPassed").Should().BeFalse();
    }
}
