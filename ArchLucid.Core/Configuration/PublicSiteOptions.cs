namespace ArchLucid.Core.Configuration;

/// <summary>Public operator / marketing site base URL (<c>ArchLucid:PublicSite</c>).</summary>
public sealed class PublicSiteOptions
{
    /// <summary>Configuration section path under the host root (<c>ArchLucid:PublicSite</c>).</summary>
    public const string SectionPath = "ArchLucid:PublicSite";

    /// <summary>HTTPS origin for deep links in emails, exports, and integration payloads (no trailing slash).</summary>
    public string BaseUrl
    {
        get;
        init;
    } = "https://archlucid.net";
}
