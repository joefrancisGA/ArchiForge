namespace ArchLucid.Application.Evidence;

/// <summary>
///     Stable identifiers for the built-in policy evidence entries injected by
///     <see cref="DefaultEvidenceBuilder" />. These IDs are stored in
///     <see cref="ArchLucid.Contracts.Agents.PolicyEvidence.PolicyId" /> and may
///     appear in agent reasoning output, so they must not change between builds.
/// </summary>
public static class BuiltInPolicyIds
{
    /// <summary>Baseline governance expectations for internal enterprise workloads.</summary>
    public const string EnterpriseDefault = "policy-enterprise-default";

    /// <summary>Policy requiring services to prefer managed identity over embedded secrets.</summary>
    public const string ManagedIdentity = "policy-managed-identity";

    /// <summary>Policy requiring private connectivity for data-bearing services.</summary>
    public const string PrivateNetworking = "policy-private-networking";

    /// <summary>Policy requiring encryption at rest for persistent storage.</summary>
    public const string EncryptionAtRest = "policy-encryption-at-rest";
}
