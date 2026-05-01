using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     <see cref="IDocumentLogoProvider" /> that reads a logo image from the local file system.
///     Returns <see langword="null" /> when logo inclusion is disabled, the path is blank, or the file does not exist.
/// </summary>
/// <remarks>
///     Relative <see cref="ConsultingDocxTemplateOptions.LogoPath" /> values resolve against
///     <see cref="System.AppContext.BaseDirectory" /> so assets copied with <c>CopyToOutputDirectory</c> load next to the
///     host assembly.
/// </remarks>
[ExcludeFromCodeCoverage(Justification =
    "Reads files from local filesystem; would require temp-file fixture with marginal value for simple guard-clause logic.")]
public sealed class FileSystemDocumentLogoProvider : IDocumentLogoProvider
{
    public async Task<byte[]?> GetLogoBytesAsync(
        ConsultingDocxTemplateOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IncludeLogo)
            return null;

        if (string.IsNullOrWhiteSpace(options.LogoPath))
            return null;

        string resolved = ResolveLogoPath(options.LogoPath);

        if (!File.Exists(resolved))
            return null;

        return await File.ReadAllBytesAsync(resolved, cancellationToken);
    }

    private static string ResolveLogoPath(string logoPath)
    {
        string trimmed = logoPath.Trim();

        return Path.IsPathRooted(trimmed)
            ? trimmed
            : Path.Combine(AppContext.BaseDirectory, trimmed.Replace('/', Path.DirectorySeparatorChar));
    }
}
