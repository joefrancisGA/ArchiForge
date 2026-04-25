namespace ArchLucid.Core.Configuration;

/// <summary>
///     Feature flags for deterministic Contoso trusted-baseline seeding (49R pass 2 / Corrected 50R). Never enable in
///     production-like environments.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Master switch for demo seed API and startup hook.</summary>
    public bool Enabled
    {
        get;
        set;
    }

    /// <summary>When <c>true</c> and the host is Development, runs demo seed once after DbUp.</summary>
    public bool SeedOnStartup
    {
        get;
        set;
    }

    /// <summary>
    ///     Absolute TTL in seconds for the in-process cache entry backing <c>GET /v1/demo/preview</c>.
    ///     Clamped to 30–3600 when applied by the controller (invalid/zero falls back to 300).
    /// </summary>
    public int PreviewCacheSeconds
    {
        get;
        set;
    } = 300;

    /// <summary>
    ///     Authority demo seed density per ADR 0030 owner Decision B (2026-04-23): <c>quickstart</c>
    ///     (one-of-each minimum — single finding, empty graph) vs <c>vertical</c> (production-realistic
    ///     depth — multiple findings + graph nodes/edges). The historical aliases <c>full</c> and
    ///     <c>production-realistic</c> remain accepted as synonyms for <c>vertical</c>; any other value
    ///     (including <c>skeleton</c>) is treated as <c>quickstart</c>.
    /// </summary>
    public string SeedDepth
    {
        get;
        set;
    } = "vertical";
}
