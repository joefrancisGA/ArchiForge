using ArchiForge.Persistence.RelationalRead;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Contract tests for <see cref="JsonFallbackPolicy"/> — the single seam that governs
/// whether persistence reads fall back to JSON columns when relational child tables are empty.
/// </summary>
[Trait("Category", "Unit")]
public sealed class JsonFallbackPolicyTests
{
    [Fact]
    public void Default_AllowFallback_IsTrue()
    {
        JsonFallbackPolicy policy = new();

        policy.AllowFallback.Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToJson_RelationalRowsExist_ReturnsFalse_RegardlessOfPolicy()
    {
        JsonFallbackPolicy allow = new() { AllowFallback = true };
        JsonFallbackPolicy deny = new() { AllowFallback = false };

        allow.ShouldFallbackToJson(5, "Test.Slice").Should().BeFalse();
        deny.ShouldFallbackToJson(1, "Test.Slice").Should().BeFalse();
    }

    [Fact]
    public void ShouldFallbackToJson_NoRelationalRows_AllowFallback_ReturnsTrue()
    {
        JsonFallbackPolicy policy = new() { AllowFallback = true };

        policy.ShouldFallbackToJson(0, "ContextSnapshot.CanonicalObjects").Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToJson_NoRelationalRows_DenyFallback_ReturnsFalse()
    {
        JsonFallbackPolicy policy = new() { AllowFallback = false };

        policy.ShouldFallbackToJson(0, "ContextSnapshot.CanonicalObjects").Should().BeFalse();
    }
}
