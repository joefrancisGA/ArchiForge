using ArchiForge.Core.Scoping;

using FluentAssertions;

namespace ArchiForge.Core.Tests.Scoping;

[Trait("Category", "Unit")]
public sealed class SqlRowLevelSecurityBypassAmbientTests
{
    [Fact]
    public void IsActive_ByDefault_IsFalse()
    {
        bool active = SqlRowLevelSecurityBypassAmbient.IsActive;

        active.Should().BeFalse();
    }

    [Fact]
    public void Enter_ThenDispose_TogglesIsActive()
    {
        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeFalse();

        IDisposable scope = SqlRowLevelSecurityBypassAmbient.Enter();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeTrue();

        scope.Dispose();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeFalse();
    }

    [Fact]
    public void NestedEnter_DisposeInnerThenOuter_RestoresDepthCorrectly()
    {
        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeFalse();

        IDisposable outer = SqlRowLevelSecurityBypassAmbient.Enter();
        IDisposable inner = SqlRowLevelSecurityBypassAmbient.Enter();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeTrue();

        inner.Dispose();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeTrue();

        outer.Dispose();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Dispose_TwiceOnSameScope_IsSafe()
    {
        IDisposable scope = SqlRowLevelSecurityBypassAmbient.Enter();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeTrue();

        scope.Dispose();
        scope.Dispose();

        SqlRowLevelSecurityBypassAmbient.IsActive.Should().BeFalse();
    }
}
