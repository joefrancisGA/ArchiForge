using ArchLucid.Api.Validators;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

using FluentValidation.Results;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="InfrastructureDeclarationRequestValidator" /> infrastructure declaration rows on architecture requests.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InfrastructureDeclarationRequestValidatorTests
{
    private readonly InfrastructureDeclarationRequestValidator _validator = new();

    private static InfrastructureDeclarationRequest ValidDecl()
    {
        return new InfrastructureDeclarationRequest { Name = "primary", Format = "json", Content = """{"resources":[]}""" };
    }

    [SkippableFact]
    public void Validate_Succeeds_ForTerraformShowJson_whenContentPresent()
    {
        InfrastructureDeclarationRequest decl = ValidDecl();
        decl.Format = "terraform-show-json";
        decl.Content = """{"values":{"root_module":{"resources":[]}}}""";

        ValidationResult result = _validator.Validate(decl);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("json")]
    [InlineData("JSON")]
    [InlineData("simple-terraform")]
    [InlineData("terraform-show-json")]
    [InlineData("Terraform-Show-JSON")]
    public void Validate_Succeeds_for_each_supported_format_case_insensitive(string format)
    {
        InfrastructureDeclarationRequest decl = ValidDecl();
        decl.Format = format;

        ValidationResult result = _validator.Validate(decl);

        result.IsValid.Should().BeTrue();
    }

    [SkippableFact]
    public void Validate_Fails_WhenFormatIsUnsupported()
    {
        InfrastructureDeclarationRequest decl = ValidDecl();
        decl.Format = "hcl-bundle";

        ValidationResult result = _validator.Validate(decl);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InfrastructureDeclarationRequest.Format));
    }
}
