namespace ArchLucid.Host.Core.Jobs;

/// <summary>
/// One-shot work unit invoked by <c>ArchLucid.Jobs.Cli --job &lt;name&gt;</c> from Azure Container Apps Jobs (or local debugging).
/// </summary>
public interface IArchLucidJob
{
    /// <summary>Stable slug matching CLI <c>--job</c> and <c>Jobs:OffloadedToContainerJobs</c> entries.</summary>
    string Name { get; }

    /// <summary>
    /// Executes a single logical iteration. Returns <see cref="ArchLucidJobExitCodes"/> values
    /// (<c>0</c> success, <c>1</c> failure).
    /// </summary>
    Task<int> RunOnceAsync(CancellationToken cancellationToken);
}
