using ArchLucid.Core.Authority;
using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Authority;

/// <summary>
/// Resolves async authority mode from <see cref="IConfiguration"/> storage provider and feature management.
/// </summary>
public sealed class FeatureManagementAuthorityPipelineModeResolver(
    IFeatureFlags featureFlags,
    IConfiguration configuration) : IAsyncAuthorityPipelineModeResolver
{
    private readonly IFeatureFlags _featureFlags =
        featureFlags ?? throw new ArgumentNullException(nameof(featureFlags));

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <inheritdoc />
    public async Task<bool> ShouldQueueContextAndGraphStagesAsync(CancellationToken cancellationToken = default)
    {
        ArchLucidOptions archLucid = ArchLucidConfigurationBridge.ResolveArchLucidOptions(_configuration);

        if (ArchLucidOptions.EffectiveIsInMemory(archLucid.StorageProvider))
            return false;

        if (!ArchLucidOptions.EffectiveIsSql(archLucid.StorageProvider))
            return false;

        return await _featureFlags.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, cancellationToken);
    }
}
