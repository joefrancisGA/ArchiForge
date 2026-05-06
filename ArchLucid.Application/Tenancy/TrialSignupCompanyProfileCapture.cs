namespace ArchLucid.Application.Tenancy;
/// <summary>Optional company profile at self-service trial signup (persisted on <c>dbo.Tenants</c> with the trial row).</summary>
public sealed record TrialSignupCompanyProfileCapture(string? CompanySize, int? ArchitectureTeamSize, string? IndustryVertical, string? IndustryVerticalOther)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(CompanySize, IndustryVertical, IndustryVerticalOther);
    private static byte __ValidatePrimaryConstructorArguments(System.String? CompanySize, System.String? IndustryVertical, System.String? IndustryVerticalOther)
    {
        return (byte)0;
    }
}