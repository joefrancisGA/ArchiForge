namespace ArchiForge.Application.Analysis;

public interface IDocumentLogoProvider
{
    Task<byte[]?> GetLogoBytesAsync(
        ConsultingDocxTemplateOptions options,
        CancellationToken cancellationToken = default);
}

