namespace ArchLucid.Core.Configuration;

/// <summary>Thin abstraction over feature flag evaluation, enabling testability and future provider swap.</summary>
public interface IFeatureFlags
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}
