namespace ArchiForge.Core.Scoping;

/// <summary>
/// Well-known scope GUIDs used as defaults during local development and integration testing.
/// </summary>
/// <remarks>
/// <strong>These values are development defaults only.</strong> They must not be relied upon
/// in production multi-tenant deployments where tenant, workspace, and project isolation is required.
/// Production scope IDs should always be resolved from the authenticated request context.
/// </remarks>
public static class ScopeIds
{
    /// <summary>
    /// Default tenant GUID for local development and integration tests.
    /// Not safe to assume in production.
    /// </summary>
    public static readonly Guid DefaultTenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Default workspace GUID for local development and integration tests.
    /// Not safe to assume in production.
    /// </summary>
    public static readonly Guid DefaultWorkspace = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// Default project GUID for local development and integration tests.
    /// Not safe to assume in production.
    /// </summary>
    public static readonly Guid DefaultProject = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
