using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.Traceability;

/// <summary>Assembles a ZIP audit hand-off bundle for a single architecture run.</summary>
public interface ITraceabilityBundleBuilder
{
    /// <summary>
    ///     Returns ZIP bytes, <see langword="null" /> when the run is missing, or throws
    ///     <see cref="TraceabilityBundleTooLargeException" />.
    /// </summary>
    Task<byte[]?> BuildAsync(string runId, ScopeContext scope, long maxZipBytes, CancellationToken cancellationToken);
}
