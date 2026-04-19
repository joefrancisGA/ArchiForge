using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Startup;

/// <summary>
/// Forces fail-closed SDK behavior for Azure Content Safety on production-like hosts.
/// </summary>
public sealed class ContentSafetyProductionLikePostConfigure(
    IHostEnvironment hostEnvironment,
    IConfiguration configuration) : IPostConfigureOptions<ContentSafetyOptions>
{
    private readonly IHostEnvironment _hostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <inheritdoc />
    public void PostConfigure(string? name, ContentSafetyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!HostEnvironmentClassification.IsProductionOrStagingLike(_hostEnvironment, _configuration))
            return;

        options.FailClosedOnSdkError = true;
    }
}
