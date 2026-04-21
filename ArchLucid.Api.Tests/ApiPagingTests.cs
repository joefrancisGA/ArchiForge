using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ApiPagingTests
{
    [Fact]
    public void TryParseUtcTicksIdCursor_null_cursor_succeeds_with_null_outputs()
    {
        bool ok = ApiPaging.TryParseUtcTicksIdCursor(null, out DateTime? createdUtc, out string? id, out string? error);

        ok.Should().BeTrue();
        createdUtc.Should().BeNull();
        id.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseUtcTicksIdCursor_whitespace_cursor_succeeds_with_null_outputs()
    {
        bool ok = ApiPaging.TryParseUtcTicksIdCursor("   ", out DateTime? createdUtc, out string? id, out string? error);

        ok.Should().BeTrue();
        createdUtc.Should().BeNull();
        id.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseUtcTicksIdCursor_invalid_format_returns_false_with_error()
    {
        bool ok = ApiPaging.TryParseUtcTicksIdCursor("bad", out DateTime? createdUtc, out string? id, out string? error);

        ok.Should().BeFalse();
        createdUtc.Should().BeNull();
        id.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryParseUtcTicksIdCursor_valid_cursor_round_trips()
    {
        DateTimeOffset expected = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
        string idPart = "rec-42";
        string cursor = $"{expected.UtcTicks}:{idPart}";

        bool ok = ApiPaging.TryParseUtcTicksIdCursor(cursor, out DateTime? createdUtc, out string? id, out string? error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        id.Should().Be(idPart);
        createdUtc.Should().NotBeNull();
        DateTime.SpecifyKind(createdUtc.Value, DateTimeKind.Utc).Should().Be(expected.UtcDateTime);
    }
}
