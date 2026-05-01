namespace ArchLucid.Application.Notifications.Email.Models;

public sealed record TrialWelcomeEmailModel(string OrganizationHint, string ProductName, string? LogoImageUrl = null);

public sealed record TrialFirstRunEmailModel(string ProductName, string GettingStartedUrl, string? LogoImageUrl = null);

public sealed record TrialMidTrialEmailModel(string ProductName, string DashboardUrl, string? LogoImageUrl = null);

public sealed record TrialApproachingRunLimitEmailModel(
    string ProductName,
    int RunsUsed,
    int RunsLimit,
    string? LogoImageUrl = null);

public sealed record TrialExpiringSoonEmailModel(string ProductName, int DaysRemaining, string? LogoImageUrl = null);

public sealed record TrialExpiredEmailModel(string ProductName, string ExportHelpUrl, string? LogoImageUrl = null);

public sealed record TrialConvertedEmailModel(string ProductName, string Tier, string? LogoImageUrl = null);
