using System.Text.RegularExpressions;

using RazorLight;

namespace ArchLucid.Application.Notifications.Email;

/// <summary>Renders embedded Razor (<c>.cshtml</c>) views shipped with <see cref="ArchLucid.Application" />.</summary>
public sealed class RazorLightEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Regex StripTags = new("<[^>]+>", RegexOptions.Compiled);

    private readonly RazorLightEngine _engine = new RazorLightEngineBuilder()
        .UseEmbeddedResourcesProject(typeof(EmailTemplateAnchor))
        .UseMemoryCachingProvider()
        .Build();

    /// <inheritdoc />
    public Task<string> RenderHtmlAsync(string templateId, object model, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        string key = TemplateKey(templateId);

        return _engine.CompileRenderAsync(key, model);
    }

    /// <inheritdoc />
    public async Task<string> RenderTextAsync(string templateId, object model, CancellationToken cancellationToken)
    {
        string html = await RenderHtmlAsync(templateId, model, cancellationToken).ConfigureAwait(false);

        return StripTags.Replace(html, " ").Trim();
    }

    /// <summary>Visible for template snapshot tests.</summary>
    internal static string TemplateKey(string templateId)
    {
        return string.IsNullOrWhiteSpace(templateId)
            ? throw new ArgumentException("Template id is required.", nameof(templateId))
            :
            // RazorLight resolves views relative to <see cref="EmailTemplateAnchor"/>'s namespace (ArchLucid.Application.Notifications.Email).
            $"Templates.{templateId.Trim()}.cshtml";
    }
}
