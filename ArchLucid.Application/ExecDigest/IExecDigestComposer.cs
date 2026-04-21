using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.ExecDigest;

/// <summary>Builds the weekly executive digest for a tenant/week using existing application services (no new SQL aggregates).</summary>
public interface IExecDigestComposer
{
    /// <summary>
    /// <paramref name="weekStartUtcInclusive"/> / <paramref name="weekEndUtcExclusive"/> define the digest window in UTC.
    /// <paramref name="authorityScope"/> must match the tenant’s primary authority project boundary (workspace + project ids).
    /// </summary>
    Task<ExecDigestComposition> ComposeAsync(
        Guid tenantId,
        DateTime weekStartUtcInclusive,
        DateTime weekEndUtcExclusive,
        ScopeContext authorityScope,
        string operatorBaseUrl,
        CancellationToken cancellationToken);
}
