using ArchLucid.Api.Filters;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Attributes;

/// <summary>
/// Marks an action or controller as part of the **coordinator pipeline being retired** by
/// <see href="../../docs/adr/0021-coordinator-pipeline-strangler-plan.md">ADR 0021</see>.
/// On every successful response the configured <see cref="CoordinatorPipelineDeprecationFilter"/>
/// emits the standards-track deprecation signal:
/// <list type="bullet">
///   <item><description><c>Deprecation: true</c> (RFC 9745 — the route is currently deprecated).</description></item>
///   <item><description><c>Sunset: &lt;RFC 1123 date&gt;</c> (RFC 8594 — earliest possible removal date, gated on Phase 3).</description></item>
///   <item><description><c>Link: &lt;adr-url&gt;; rel="deprecation"; type="text/markdown"</c> (RFC 8288 — pointer to ADR 0021).</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Mirrors the <see cref="FeatureGateAttribute"/> shape: a thin <see cref="TypeFilterAttribute"/>
/// that applies the per-action filter so other dependencies stay DI-resolved. The deprecation
/// signal is route-scoped (only the targeted actions emit it) so the global
/// <c>ApiDeprecationHeadersMiddleware</c> remains free for whole-API version-level deprecations.
/// </remarks>
public sealed class CoordinatorPipelineDeprecatedAttribute : TypeFilterAttribute
{
    public CoordinatorPipelineDeprecatedAttribute()
        : base(typeof(CoordinatorPipelineDeprecationFilter))
    {
    }
}
