using ArchLucid.Application.Identity;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Identity;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TrialPasswordPolicyValidatorTests
{
    [SkippableFact]
    public void Validate_null_password_fails()
    {
        TrialPasswordPolicyValidator sut = CreateSut(min: 8, max: 128);

        TrialPasswordValidationResult r = sut.Validate(null!);

        r.Ok.Should().BeFalse();
        r.ErrorMessage!.ToLowerInvariant().Should().Contain("required");
    }

    [SkippableFact]
    public void Validate_below_minimum_length_fails()
    {
        TrialPasswordPolicyValidator sut = CreateSut(min: 12, max: 128);

        TrialPasswordValidationResult r = sut.Validate("short");

        r.Ok.Should().BeFalse();
        r.ErrorMessage!.Should().Contain("12");
    }

    [SkippableFact]
    public void Validate_above_maximum_length_fails()
    {
        TrialPasswordPolicyValidator sut = CreateSut(min: 8, max: 10);

        TrialPasswordValidationResult r = sut.Validate(new string('x', 11));

        r.Ok.Should().BeFalse();
        r.ErrorMessage!.Should().Contain("10");
    }

    [SkippableFact]
    public void Validate_length_only_accepts_password_without_composition_rules()
    {
        TrialPasswordPolicyValidator sut = CreateSut(min: 8, max: 128);

        TrialPasswordValidationResult r = sut.Validate("alllowercase");

        r.Ok.Should().BeTrue();
        r.ErrorMessage.Should().BeNull();
    }

    private static TrialPasswordPolicyValidator CreateSut(int min, int max)
    {
        TrialAuthOptions options = new() { LocalIdentity = new TrialLocalIdentityOptions { MinimumPasswordLength = min, MaximumPasswordLength = max }, };

        Mock<IOptions<TrialAuthOptions>> mo = new();
        mo.Setup(x => x.Value).Returns(options);

        return new TrialPasswordPolicyValidator(mo.Object);
    }
}
