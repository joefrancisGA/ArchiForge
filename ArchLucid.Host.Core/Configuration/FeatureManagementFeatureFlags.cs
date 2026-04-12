using ArchLucid.Core.Configuration;

using Microsoft.FeatureManagement;

namespace ArchLucid.Host.Core.Configuration;

/// <summary>Delegates to <see cref="IFeatureManager"/> from Microsoft.FeatureManagement.</summary>
public sealed class FeatureManagementFeatureFlags(IFeatureManager featureManager) : IFeatureFlags
{
    private readonly IFeatureManager _featureManager =
        featureManager ?? throw new ArgumentNullException(nameof(featureManager));

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

        return _featureManager.IsEnabledAsync(featureName, cancellationToken);
    }
}
