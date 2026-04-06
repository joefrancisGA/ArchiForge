using System.Text;

using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api;

public static class ApiFileResults
{
    public static IActionResult RangeText(
        HttpRequest request,
        string? content,
        string contentType,
        string fileName)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        return new FileWithRangeResult(request, bytes, contentType, fileName);
    }

    public static IActionResult RangeBytes(
        HttpRequest request,
        byte[]? bytes,
        string contentType,
        string fileName)
    {
        return new FileWithRangeResult(request, bytes ?? [], contentType, fileName);
    }

    /// <summary>Standard download without range support (e.g. small ZIP payloads).</summary>
    public static IActionResult SimpleBytes(byte[]? bytes, string contentType, string fileName) =>
        new FileContentResult(bytes ?? [], contentType) { FileDownloadName = fileName };
}

