namespace ArchLucid.TestSupport;

/// <summary>
/// Maximum parent-directory steps when resolving repo artifacts from test assembly <c>bin</c> output
/// (avoids infinite walk if layout changes).
/// </summary>
public static class TestRepositoryPathLimits
{
    /// <summary>Locating <c>*.sln</c> from architecture / NetArchTest output (deep CI workspaces).</summary>
    public const int MaxStepsFromTestAssemblyBinToSolutionFile = 24;

    /// <summary>Locating a project folder or <c>Contracts</c> from API test <c>bin</c> output.</summary>
    public const int MaxStepsFromTestAssemblyBinToProjectOrContracts = 12;
}
