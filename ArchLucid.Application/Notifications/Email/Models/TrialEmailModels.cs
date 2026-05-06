namespace ArchLucid.Application.Notifications.Email.Models;
public sealed record TrialWelcomeEmailModel(string OrganizationHint, string ProductName, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(OrganizationHint, ProductName, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String OrganizationHint, System.String ProductName, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(OrganizationHint);
        ArgumentNullException.ThrowIfNull(ProductName);
        return (byte)0;
    }
}

public sealed record TrialFirstRunEmailModel(string ProductName, string GettingStartedUrl, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ProductName, GettingStartedUrl, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String ProductName, System.String GettingStartedUrl, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(ProductName);
        ArgumentNullException.ThrowIfNull(GettingStartedUrl);
        return (byte)0;
    }
}

public sealed record TrialMidTrialEmailModel(string ProductName, string DashboardUrl, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ProductName, DashboardUrl, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String ProductName, System.String DashboardUrl, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(ProductName);
        ArgumentNullException.ThrowIfNull(DashboardUrl);
        return (byte)0;
    }
}

public sealed record TrialApproachingRunLimitEmailModel(string ProductName, int RunsUsed, int RunsLimit, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ProductName, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String ProductName, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(ProductName);
        return (byte)0;
    }
}

public sealed record TrialExpiringSoonEmailModel(string ProductName, int DaysRemaining, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ProductName, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String ProductName, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(ProductName);
        return (byte)0;
    }
}

public sealed record TrialExpiredEmailModel(string ProductName, string ExportHelpUrl, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ProductName, ExportHelpUrl, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String ProductName, System.String ExportHelpUrl, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(ProductName);
        ArgumentNullException.ThrowIfNull(ExportHelpUrl);
        return (byte)0;
    }
}

public sealed record TrialConvertedEmailModel(string ProductName, string Tier, string? LogoImageUrl = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ProductName, Tier, LogoImageUrl);
    private static byte __ValidatePrimaryConstructorArguments(System.String ProductName, System.String Tier, System.String? LogoImageUrl)
    {
        ArgumentNullException.ThrowIfNull(ProductName);
        ArgumentNullException.ThrowIfNull(Tier);
        return (byte)0;
    }
}