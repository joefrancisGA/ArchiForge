using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Runs;
/// <summary>Thrown when optional pre-commit governance blocks manifest commit.</summary>
public sealed class PreCommitGovernanceBlockedException : Exception
{
    public PreCommitGovernanceBlockedException(PreCommitGateResult result) : base(result.Reason ?? "Commit blocked by governance policy.")
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public PreCommitGateResult Result { get; }
}