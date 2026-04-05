using ArchiForge.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Unit tests for <see cref="ReplayDiagnosticsRecorder"/> capacity and retention trimming.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReplayDiagnosticsRecorderTests
{
    [Fact]
    public void Record_respects_capacity()
    {
        Mock<IOptionsMonitor<ReplayDiagnosticsOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new ReplayDiagnosticsOptions { Capacity = 2, RetentionMinutes = 0 });
        ReplayDiagnosticsRecorder sut = new(options.Object);

        sut.Record(NewEntry("a"));
        sut.Record(NewEntry("b"));
        sut.Record(NewEntry("c"));

        sut.GetRecent(10).Should().HaveCount(2);
        sut.GetRecent(10)[0].ComparisonRecordId.Should().Be("b");
        sut.GetRecent(10)[1].ComparisonRecordId.Should().Be("c");
    }

    [Fact]
    public void Record_evicts_entries_older_than_retention_minutes()
    {
        Mock<IOptionsMonitor<ReplayDiagnosticsOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new ReplayDiagnosticsOptions { Capacity = 100, RetentionMinutes = 60 });
        ReplayDiagnosticsRecorder sut = new(options.Object);

        sut.Record(new ReplayDiagnosticsEntry
        {
            TimestampUtc = DateTime.UtcNow.AddHours(-2),
            ComparisonRecordId = "old"
        });
        sut.Record(new ReplayDiagnosticsEntry
        {
            TimestampUtc = DateTime.UtcNow,
            ComparisonRecordId = "new"
        });

        IReadOnlyList<ReplayDiagnosticsEntry> recent = sut.GetRecent(10);
        recent.Should().ContainSingle(e => e.ComparisonRecordId == "new");
        recent.Should().NotContain(e => e.ComparisonRecordId == "old");
    }

    private static ReplayDiagnosticsEntry NewEntry(string id)
    {
        return new ReplayDiagnosticsEntry
        {
            TimestampUtc = DateTime.UtcNow,
            ComparisonRecordId = id
        };
    }
}
