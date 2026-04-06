namespace ArchiForge.Host.Core.Configuration;

/// <summary>
/// Observability configuration under section <c>Observability</c> (OTel exporters, Prometheus scrape).
/// </summary>
public sealed class ObservabilityHostOptions
{
    public const string SectionName = "Observability";

    /// <summary>Prometheus scrape endpoint and optional Basic auth (child keys under <c>Observability:Prometheus</c>).</summary>
    public ObservabilityPrometheusOptions Prometheus { get; set; } = new();
}

/// <summary>Binding for <c>Observability:Prometheus</c>.</summary>
public sealed class ObservabilityPrometheusOptions
{
    public bool Enabled { get; set; }

    /// <summary>HTTP path served by the OpenTelemetry Prometheus exporter.</summary>
    public string ScrapePath { get; set; } = "/metrics";

    /// <summary>When true (default), startup validation requires scrape credentials whenever Prometheus is enabled.</summary>
    public bool RequireScrapeAuthentication { get; set; } = true;

    public string? ScrapeUsername { get; set; }

    public string? ScrapePassword { get; set; }
}
