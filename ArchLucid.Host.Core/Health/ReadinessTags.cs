namespace ArchiForge.Host.Core.Health;

/// <summary>Tags for ASP.NET Core health checks: liveness vs readiness probes.</summary>
public static class ReadinessTags
{
    public const string Live = "live";
    public const string Ready = "ready";
}
