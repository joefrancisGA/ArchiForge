namespace ArchLucid.Application.Bootstrap;

/// <summary>Stable keys for the Contoso Retail Modernization trusted-baseline demo (49R pass 2 / Corrected 50R).</summary>
public static class ContosoRetailDemoIdentifiers
{
    public const string RequestContoso = "request-contoso-demo";

    /// <summary>Authority <c>dbo.Runs.RunId</c> (coordinator string key is the same value as <see cref="Guid.ToString"/> <c>"N"</c>).</summary>
    public static readonly Guid AuthorityRunBaselineId = Guid.Parse("6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501");

    /// <summary>Second demo authority run (hardened manifest path).</summary>
    public static readonly Guid AuthorityRunHardenedId = Guid.Parse("6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c502");

    /// <summary>Legacy string run key matching <see cref="Coordinator.Services.CoordinatorService"/> (authority GUID, no dashes).</summary>
    public static string RunBaseline { get; } = AuthorityRunBaselineId.ToString("N");

    /// <inheritdoc cref="RunBaseline" path="/summary" />
    public static string RunHardened { get; } = AuthorityRunHardenedId.ToString("N");
    public const string ManifestBaseline = "contoso-baseline-v1";
    public const string ManifestHardened = "contoso-hardened-v1";
    public const string ApprovalRequest = "apr-demo-001";
    public const string PromotionRecord = "promo-demo-001";
    public const string ActivationDev = "act-demo-dev-001";
    public const string ActivationTest = "act-demo-test-001";
    public const string ExportRecord = "export-demo-baseline-001";
}
