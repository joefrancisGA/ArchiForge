using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// <see cref="IDocumentLogoProvider"/> that reads a logo image from the local file system.
/// Returns <see langword="null"/> when logo inclusion is disabled, the path is blank, or the file does not exist.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Reads files from local filesystem; would require temp-file fixture with marginal value for simple guard-clause logic.")]
public sealed class FileSystemDocumentLogoProvider : IDocumentLogoProvider
{
    public async Task<byte[]?> GetLogoBytesAsync(
        ConsultingDocxTemplateOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IncludeLogo)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.LogoPath))
        {
            return null;
        }

        if (!File.Exists(options.LogoPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(options.LogoPath, cancellationToken);
    }
}

