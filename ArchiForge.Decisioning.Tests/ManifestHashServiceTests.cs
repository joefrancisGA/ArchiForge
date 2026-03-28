using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Services;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

[Trait("Suite", "Core")]
public sealed class ManifestHashServiceTests
{
    private readonly ManifestHashService _sut = new();

    private static GoldenManifest BaseManifest() => new()
    {
        ManifestId = new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
        RunId = new Guid("bbbbbbbb-0000-0000-0000-000000000002"),
        TenantId = new Guid("cccccccc-0000-0000-0000-000000000003"),
        WorkspaceId = new Guid("dddddddd-0000-0000-0000-000000000004"),
        ProjectId = new Guid("eeeeeeee-0000-0000-0000-000000000005"),
        RuleSetId = "default-v1",
        RuleSetVersion = "1.0",
        RuleSetHash = "abc123",
        Policy = new PolicySection(),
        Provenance = new ManifestProvenance()
    };

    [Fact]
    public void ComputeHash_SameManifest_ReturnsSameHash()
    {
        GoldenManifest a = BaseManifest();
        GoldenManifest b = BaseManifest();

        string hashA = _sut.ComputeHash(a);
        string hashB = _sut.ComputeHash(b);

        hashA.Should().Be(hashB);
    }

    [Fact]
    public void ComputeHash_DifferentManifestId_ReturnsDifferentHash()
    {
        GoldenManifest a = BaseManifest();
        GoldenManifest b = BaseManifest();
        b.ManifestId = Guid.NewGuid();

        _sut.ComputeHash(a).Should().NotBe(_sut.ComputeHash(b));
    }

    [Fact]
    public void ComputeHash_PolicySectionAffectsHash()
    {
        GoldenManifest withEmptyPolicy = BaseManifest();
        GoldenManifest withViolation = BaseManifest();
        withViolation.Policy.Violations.Add(new PolicyControlItem
        {
            ControlId = "CTRL-001",
            ControlName = "Encryption at rest"
        });

        string hashEmpty = _sut.ComputeHash(withEmptyPolicy);
        string hashWithViolation = _sut.ComputeHash(withViolation);

        hashEmpty.Should().NotBe(hashWithViolation);
    }

    [Fact]
    public void ComputeHash_PolicySatisfiedControlAffectsHash()
    {
        GoldenManifest withoutControl = BaseManifest();
        GoldenManifest withControl = BaseManifest();
        withControl.Policy.SatisfiedControls.Add(new PolicyControlItem
        {
            ControlId = "CTRL-002",
            ControlName = "MFA enforced"
        });

        _sut.ComputeHash(withoutControl).Should().NotBe(_sut.ComputeHash(withControl));
    }

    [Fact]
    public void ComputeHash_NullManifest_Throws()
    {
        Action act = () => _sut.ComputeHash(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeHash_ReturnsUpperHexString()
    {
        string hash = _sut.ComputeHash(BaseManifest());

        hash.Should().MatchRegex("^[0-9A-F]{64}$");
    }
}
