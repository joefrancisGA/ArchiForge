using ArchLucid.Api.Validators;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Replay Validation Constants.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReplayValidationConstantsTests
{
    [Fact]
    public void ValidFormats_contains_expected_values_and_count()
    {
        string[] expected = ["markdown", "html", "docx", "json"];
        ReplayValidationConstants.ValidFormats.Should().HaveCount(expected.Length);
        foreach (string e in expected)
            ReplayValidationConstants.ValidFormats.Should().Contain(e);
    }

    [Fact]
    public void ValidReplayModes_contains_expected_values_and_count()
    {
        string[] expected = ["artifact", "regenerate", "verify"];
        ReplayValidationConstants.ValidReplayModes.Should().HaveCount(expected.Length);
        foreach (string e in expected)
            ReplayValidationConstants.ValidReplayModes.Should().Contain(e);
    }

    [Fact]
    public void ValidProfiles_contains_expected_values_and_count()
    {
        string[] expected = ["default", "short", "detailed", "executive"];
        ReplayValidationConstants.ValidProfiles.Should().HaveCount(expected.Length);
        foreach (string e in expected)
            ReplayValidationConstants.ValidProfiles.Should().Contain(e);
    }

    [Fact]
    public void ValidFormats_is_case_insensitive_for_lookup()
    {
        ReplayValidationConstants.ValidFormats.Should().Contain("MARKDOWN");
        ReplayValidationConstants.ValidFormats.Should().Contain("DocX");
    }
}
