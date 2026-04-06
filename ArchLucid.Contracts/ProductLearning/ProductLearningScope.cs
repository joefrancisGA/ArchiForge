namespace ArchiForge.Contracts.ProductLearning;

/// <summary>Tenant/workspace/project boundary for product-learning queries (matches signal rows).</summary>
public sealed class ProductLearningScope
{
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
}
