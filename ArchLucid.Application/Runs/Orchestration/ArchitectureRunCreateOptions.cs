namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Options for <see cref="ArchitectureRunCreateOrchestrator" /> create-run behaviour.
/// </summary>
public sealed class ArchitectureRunCreateOptions
{
    /// <summary>Configuration section: <c>ArchLucid:CreateRun</c>.</summary>
    public const string SectionPath = "ArchLucid:CreateRun";

    /// <summary>
    ///     Milliseconds passed to SQL Server <c>sp_getapplock</c> <c>@LockTimeout</c> while waiting for the same
    ///     <c>Idempotency-Key</c> create-run to finish. Parallel bursts share one winner; losers must wait for that
    ///     transaction without timing out on cold or slow SQL hosts.
    /// </summary>
    /// <remarks>
    ///     Clamped in <see cref="ArchitectureRunCreateOrchestrator" /> to <c>[1000, 600000]</c>.
    /// </remarks>
    public int DistributedIdempotencyLockTimeoutMilliseconds
    {
        get;
        set;
    } = 120_000;
}
