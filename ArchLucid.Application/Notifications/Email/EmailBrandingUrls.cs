namespace ArchLucid.Application.Notifications.Email;
/// <summary>Builds absolute asset URLs for HTML email templates (logos must be raster; many clients block SVG in img).</summary>
public static class EmailBrandingUrls
{
    /// <summary>Default logo under the operator static site: PNG app tile.</summary>
    public const string DefaultLogoRelativePath = "/logo/icon-192.png";
    /// <summary>Returns <see langword="null"/> when <paramref name = "operatorBaseUrl"/> is blank.</summary>
    public static System.String? TryBuildLogoImageUrl(string? operatorBaseUrl, string relativePath = DefaultLogoRelativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        if (string.IsNullOrWhiteSpace(operatorBaseUrl))
            return null;
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;
        string trimmedBase = operatorBaseUrl.TrimEnd('/');
        string rel = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return $"{trimmedBase}{rel}";
    }
}