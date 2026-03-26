using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Services;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class GoldenManifestValidatorTests
{
    private readonly GoldenManifestValidator _sut = new();

    /// <summary>
    /// Returns a manifest with all required fields populated so individual tests
    /// can null out exactly one section at a time.
    /// </summary>
    private static GoldenManifest ValidManifest() => new()
    {
        ManifestId = Guid.NewGuid(),
        RunId = Guid.NewGuid(),
        RuleSetId = "default-v1",
        Requirements = new RequirementsCoverageSection(),
        Topology = new TopologySection(),
        Security = new SecuritySection(),
        Compliance = new ComplianceSection(),
        Cost = new CostSection(),
        Policy = new PolicySection(),
        Provenance = new ManifestProvenance()
    };

    [Fact]
    public void Validate_ValidManifest_DoesNotThrow()
    {
        Action act = () => _sut.Validate(ValidManifest());

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EmptyManifestId_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.ManifestId = Guid.Empty;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ManifestId*");
    }

    [Fact]
    public void Validate_EmptyRunId_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.RunId = Guid.Empty;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RunId*");
    }

    [Fact]
    public void Validate_MissingRuleSetId_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.RuleSetId = "  ";

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RuleSetId*");
    }

    [Fact]
    public void Validate_NullRequirements_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Requirements = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Requirements*");
    }

    [Fact]
    public void Validate_NullTopology_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Topology = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Topology*");
    }

    [Fact]
    public void Validate_NullSecurity_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Security = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Security*");
    }

    [Fact]
    public void Validate_NullCompliance_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Compliance = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Compliance*");
    }

    [Fact]
    public void Validate_NullCost_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Cost = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cost*");
    }

    [Fact]
    public void Validate_NullProvenance_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Provenance = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Provenance*");
    }

    [Fact]
    public void Validate_NullPolicy_Throws()
    {
        GoldenManifest manifest = ValidManifest();
        manifest.Policy = null!;

        Action act = () => _sut.Validate(manifest);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Policy*");
    }
}
