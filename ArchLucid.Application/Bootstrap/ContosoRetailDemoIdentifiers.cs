namespace ArchLucid.Application.Bootstrap;

/// <summary>
///     Canonical single-catalog keys for the Contoso Retail Modernization trusted-baseline demo (49R pass 2 / Corrected
///     50R).
///     For multi-tenant SQL catalogs, persisted demo rows must use <see cref="ContosoRetailDemoIds.ForTenant" /> instead
///     (global PKs on <c>dbo.Runs</c>, <c>dbo.ArchitectureRequests</c>, <c>dbo.AgentTasks</c>, etc.).
/// </summary>
public static class ContosoRetailDemoIdentifiers
{
    public const string RequestContoso = "request-contoso-demo";
    public const string ManifestBaseline = "contoso-baseline-v1";
    public const string ManifestHardened = "contoso-hardened-v1";
    public const string ApprovalRequest = "apr-demo-001";
    public const string PromotionRecord = "promo-demo-001";
    public const string ActivationDev = "act-demo-dev-001";
    public const string ActivationTest = "act-demo-test-001";
    public const string ExportRecord = "export-demo-baseline-001";

    /// <summary>
    ///     Prefix used by <see cref="ContosoRetailDemoIds.ForTenant" /> when the seed runs in a non-default tenant
    ///     (e.g. a brand-new self-service tenant in the same SQL catalog). The canonical single-catalog request id
    ///     is exposed as <see cref="RequestContoso" />; multi-tenant request ids carry the tenant suffix.
    /// </summary>
    public const string MultiTenantRequestPrefix = "req-contoso-demo-";

    /// <summary>
    ///     Canonical authority <c>dbo.Runs.RunId</c> for brownfield fixtures and documentation (not used for new
    ///     multi-tenant seeds).
    /// </summary>
    public static readonly Guid AuthorityRunBaselineId = Guid.Parse("6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501");

    /// <summary>Canonical second demo authority run (hardened manifest path).</summary>
    public static readonly Guid AuthorityRunHardenedId = Guid.Parse("6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c502");

    /// <summary>Canonical legacy string run key (authority GUID, no dashes).</summary>
    public static string RunBaseline
    {
        get;
    } = AuthorityRunBaselineId.ToString("N");

    /// <inheritdoc cref="RunBaseline" path="/summary" />
    public static string RunHardened
    {
        get;
    } = AuthorityRunHardenedId.ToString("N");

    /// <summary>
    ///     <see langword="true" /> when <paramref name="runId" /> matches one of the canonical Contoso Retail demo run
    ///     identifiers (baseline or hardened). Used by sponsor-facing reports to flag computed lines as "demo tenant
    ///     — replace before publishing" so the numbers cannot be quoted out of context.
    /// </summary>
    public static bool IsDemoRunId(string? runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return false;

        return string.Equals(runId, RunBaseline, StringComparison.OrdinalIgnoreCase)
               || string.Equals(runId, RunHardened, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     <see langword="true" /> when <paramref name="requestId" /> matches the canonical demo request id or the
    ///     per-tenant multi-catalog prefix produced by <see cref="ContosoRetailDemoIds.ForTenant" />. Complements
    ///     <see cref="IsDemoRunId" /> so a tenant-scoped demo seed (which derives a fresh GUID-based RunId) is still
    ///     recognized as a demo dataset by the sponsor reports.
    /// </summary>
    public static bool IsDemoRequestId(string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return false;
        return string.Equals(requestId, RequestContoso, StringComparison.OrdinalIgnoreCase) ||
               requestId.StartsWith(MultiTenantRequestPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
