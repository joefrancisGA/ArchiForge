using Microsoft.AspNetCore.Http;

namespace ArchLucid.Application.Import;

public interface IImportRequestFileService
{
    /// <summary>
    /// Imports a UTF-8 TOML/JSON file (≤ 512 KB), validates, runs content-safety precheck, persists draft, writes audit.
    /// </summary>
    Task<ImportRequestFileResult> ImportAsync(IFormFile file, CancellationToken ct);
}
