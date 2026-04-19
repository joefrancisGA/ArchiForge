namespace ArchLucid.Host.Core.Hosting;

/// <summary>
/// Interprets <c>ASPNETCORE_URLS</c> for HTTP pipeline decisions (e.g. Docker images that bind <c>http://+:8080</c> only).
/// </summary>
public static class AspNetCoreHostingUrls
{
    /// <summary>
    /// When <c>ASPNETCORE_URLS</c> is unset, returns <see langword="true"/> (preserve default HTTPS redirection behavior).
    /// When it is set and every entry is <c>http://</c>, returns <see langword="false"/> so plain-HTTP probes (CI, containers) work.
    /// </summary>
    public static bool ShouldUseHttpsRedirection(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? urls = configuration["ASPNETCORE_URLS"]?.Trim();

        if (string.IsNullOrWhiteSpace(urls))
        {
            return true;
        }

        return urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(part => part.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }
}
