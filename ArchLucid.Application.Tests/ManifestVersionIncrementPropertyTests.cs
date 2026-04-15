using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Application.Tests;

/// <summary>Property tests for <see cref="ManifestVersionIncrementRules"/>.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ManifestVersionIncrementPropertyTests
{
    [Property(MaxTest = 200)]
    public void IncrementManifestVersion_vN_yields_vN_plus_one(PositiveInt n)
    {
        int v = n.Get % 10_000 + 1;
        string current = "v" + v;
        string next = ManifestVersionIncrementRules.IncrementManifestVersion(current);

        next.Should().Be("v" + (v + 1));
    }

    [Property(MaxTest = 200)]
    public void IncrementManifestVersion_is_case_insensitive_on_v_prefix(PositiveInt n)
    {
        int v = n.Get % 100 + 1;
        string lower = ManifestVersionIncrementRules.IncrementManifestVersion("v" + v);
        string upper = ManifestVersionIncrementRules.IncrementManifestVersion("V" + v);

        lower.Should().Be(upper);
    }

    [Fact]
    public void BuildManifestVersionForCommit_uses_v1_runId_when_no_current_version()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun run = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = null,
        };

        string built = ManifestVersionIncrementRules.BuildManifestVersionForCommit(run, runId);

        built.Should().Be("v1-" + runId);
    }

    [Fact]
    public void IncrementManifestVersion_throws_for_non_vN_strings()
    {
        Action act1 = () => ManifestVersionIncrementRules.IncrementManifestVersion("1.0.0");
        Action act2 = () => ManifestVersionIncrementRules.IncrementManifestVersion("v");
        Action act3 = () => ManifestVersionIncrementRules.IncrementManifestVersion("vX");
        Action act4 = () => ManifestVersionIncrementRules.IncrementManifestVersion("abc");

        act1.Should().Throw<InvalidOperationException>();
        act2.Should().Throw<InvalidOperationException>();
        act3.Should().Throw<InvalidOperationException>();
        act4.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IncrementManifestVersion_empty_yields_v1()
    {
        ManifestVersionIncrementRules.IncrementManifestVersion(string.Empty).Should().Be("v1");
        ManifestVersionIncrementRules.IncrementManifestVersion("   ").Should().Be("v1");
    }

    [Fact]
    public void BuildManifestVersionForCommit_increments_when_current_version_set()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun run = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v3",
        };

        ManifestVersionIncrementRules.BuildManifestVersionForCommit(run, runId).Should().Be("v4");
    }
}
