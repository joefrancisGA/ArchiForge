using ArchiForge.Core.Authority;
using ArchiForge.Host.Core.Configuration;

using Microsoft.FeatureManagement;

namespace ArchiForge.Host.Core.Authority;

/// <summary>
/// Resolves async authority mode from <see cref="IConfiguration"/> storage provider and feature management.
/// </summary>
public sealed class FeatureManagementAuthorityPipelineModeResolver(
    IFeatureManager featureManager,
    IConfiguration configuration) : IAsyncAuthorityPipelineModeResolver
{
    private readonly IFeatureManager _featureManager =
        featureManager ?? throw new ArgumentNullException(nameof(featureManager));

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <inheritdoc />
    public async Task<bool> ShouldQueueContextAndGraphStagesAsync(CancellationToken cancellationToken = default)
    {
        string? storage = _configuration["ArchiForge:StorageProvider"]?.Trim();

        if (string.Equals(storage, "InMemory", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(storage, "Sql", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(storage))
            return false;

        return await _featureManager.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, cancellationToken);
    }
}
