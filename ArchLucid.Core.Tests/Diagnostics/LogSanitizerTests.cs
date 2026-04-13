using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Category", "Unit")]
public sealed class LogSanitizerTests
{
    [Fact]
    public void Sanitize_null_returns_empty()
    {
        string result = LogSanitizer.Sanitize(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_empty_returns_empty()
    {
        string result = LogSanitizer.Sanitize(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_clean_string_returns_same_instance()
    {
        string input = new(['a', 'b', 'c']);

        string result = LogSanitizer.Sanitize(input);

        object.ReferenceEquals(result, input).Should().BeTrue();
    }

    [Fact]
    public void Sanitize_strips_newlines()
    {
        string result = LogSanitizer.Sanitize("line1\nline2\rline3");

        result.Should().Be("line1_line2_line3");
    }

    [Fact]
    public void Sanitize_strips_tab()
    {
        string result = LogSanitizer.Sanitize("a\tb");

        result.Should().Be("a_b");
    }

    [Fact]
    public void Sanitize_strips_null_char()
    {
        string result = LogSanitizer.Sanitize("a\0b");

        result.Should().Be("a_b");
    }

    [Fact]
    public void Sanitize_preserves_unicode()
    {
        const string input = "café 日本語 öäü";

        string result = LogSanitizer.Sanitize(input);

        result.Should().Be(input);
        object.ReferenceEquals(result, input).Should().BeTrue();
    }
}
