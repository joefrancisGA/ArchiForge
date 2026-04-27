namespace ArchLucid.Core.Configuration;

/// <summary>Outbound transactional email (<c>Email:*</c> configuration section).</summary>
public sealed class EmailNotificationOptions
{
    public const string SectionName = "Email";

    /// <summary>Noop (default), Smtp, AzureCommunicationServices.</summary>
    public string Provider
    {
        get;
        init;
    } = EmailProviderNames.Noop;

    public string? AzureCommunicationServicesEndpoint
    {
        get;
        init;
    }

    /// <summary>Optional user-assigned managed identity client id for ACS token acquisition.</summary>
    public string? AzureManagedIdentityClientId
    {
        get;
        init;
    }

    public string? SmtpHost
    {
        get;
        init;
    }

    public int SmtpPort
    {
        get;
        init;
    } = 25;

    public string? SmtpUser
    {
        get;
        init;
    }

    public string? SmtpPassword
    {
        get;
        init;
    }

    public string? FromAddress
    {
        get;
        init;
    }

    public string? FromDisplayName
    {
        get;
        init;
    }

    /// <summary>HTTPS base for operator links in templates (e.g. https://app.example.com).</summary>
    public string? OperatorBaseUrl
    {
        get;
        init;
    }

    /// <summary>Product label used in templates (defaults to ArchLucid when unset).</summary>
    public string? ProductDisplayName
    {
        get;
        init;
    }

    /// <summary>Inbox for <c>POST /v1/marketing/pricing/quote-request</c> notifications (sales follow-up).</summary>
    public string? PricingQuoteSalesInbox
    {
        get;
        init;
    } = "sales@archlucid.net";
}

/// <summary>Stable <see cref="EmailNotificationOptions.Provider" /> literals.</summary>
public static class EmailProviderNames
{
    public const string Noop = "Noop";

    public const string Smtp = "Smtp";

    public const string AzureCommunicationServices = "AzureCommunicationServices";
}
