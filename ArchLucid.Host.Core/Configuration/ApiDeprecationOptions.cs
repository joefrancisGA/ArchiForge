namespace ArchiForge.Host.Core.Configuration;

/// <summary>Optional deprecation signals for all HTTP responses (RFC 9745 <c>Deprecation</c>, RFC 8594 <c>Sunset</c>).</summary>
public sealed class ApiDeprecationOptions
{
    public const string SectionName = "ApiDeprecation";

    /// <summary>When true, middleware may add deprecation-related response headers.</summary>
    public bool Enabled { get; set; }

    /// <summary>When true and <see cref="Enabled"/> is true, adds <c>Deprecation: true</c>.</summary>
    public bool EmitDeprecationTrue { get; set; } = true;

    /// <summary>Optional RFC 1123 HTTP-date for the <c>Sunset</c> header (omit when empty).</summary>
    public string? SunsetHttpDate { get; set; }

    /// <summary>Optional full <c>Link</c> header value (e.g. <c>&lt;https://docs/...&gt;; rel="deprecation"</c>).</summary>
    public string? Link { get; set; }
}
