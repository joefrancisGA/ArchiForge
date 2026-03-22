namespace ArchiForge.Decisioning.Governance.PolicyPacks;

public interface IPolicyPackVersionRepository
{
    Task CreateAsync(PolicyPackVersion version, CancellationToken ct);

    Task UpdateAsync(PolicyPackVersion version, CancellationToken ct);

    Task<PolicyPackVersion?> GetByPackAndVersionAsync(
        Guid policyPackId,
        string version,
        CancellationToken ct);

    Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(
        Guid policyPackId,
        CancellationToken ct);
}
