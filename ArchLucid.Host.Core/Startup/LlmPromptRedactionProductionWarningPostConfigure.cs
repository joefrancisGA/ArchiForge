using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Startup;

/// <summary>Emits a startup warning when outbound LLM prompt redaction is disabled on production-like hosts.</summary>
public sealed class LlmPromptRedactionProductionWarningPostConfigure(
    IHostEnvironment hostEnvironment,
    IConfiguration configuration,
    ILogger<LlmPromptRedactionProductionWarningPostConfigure> logger) : IPostConfigureOptions<LlmPromptRedactionOptions>
{
    private readonly IHostEnvironment _hostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly ILogger<LlmPromptRedactionProductionWarningPostConfigure> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public void PostConfigure(string? name, LlmPromptRedactionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Enabled)
            return;


        if (!HostEnvironmentClassification.IsProductionOrStagingLike(_hostEnvironment, _configuration))
            return;


        if (_logger.IsEnabled(LogLevel.Warning))

            _logger.LogWarning(
                "LlmPromptRedaction:Enabled=false on a production-like host. Outbound prompts and trace blobs are not deny-list redacted.");
    }
}
