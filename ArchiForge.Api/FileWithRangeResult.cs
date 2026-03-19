using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api;

/// <summary>
/// Returns file content with support for HTTP Range requests (RFC 7233).
/// Sets Accept-Ranges: bytes and responds with 206 Partial Content when a valid Range header is present.
/// </summary>
public sealed class FileWithRangeResult(
    HttpRequest request,
    byte[] fileContents,
    string contentType,
    string fileDownloadName)
    : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.Headers["Accept-Ranges"] = "bytes";

        var totalLength = fileContents.LongLength;
        if (totalLength == 0)
        {
            response.ContentLength = 0;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = contentType;
            response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileDownloadName}\"";
            return;
        }

        var range = ParseRange(request.Headers.Range, totalLength);
        if (range is { Start: var start, End: var end })
        {
            var length = end - start + 1;
            response.StatusCode = StatusCodes.Status206PartialContent;
            response.Headers["Content-Range"] = $"bytes {start}-{end}/{totalLength}";
            response.ContentLength = length;
            response.ContentType = contentType;
            response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileDownloadName}\"";

            await response.Body.WriteAsync(fileContents.AsMemory((int)start, (int)length), context.HttpContext.RequestAborted);
            return;
        }

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentLength = totalLength;
        response.ContentType = contentType;
        response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileDownloadName}\"";
        await response.Body.WriteAsync(fileContents, context.HttpContext.RequestAborted);
    }

    /// <summary>
    /// Parses "bytes=start-end" (inclusive). Returns null if header is missing, invalid, or out of range.
    /// </summary>
    private static (long Start, long End)? ParseRange(string? rangeHeader, long totalLength)
    {
        if (string.IsNullOrWhiteSpace(rangeHeader) || totalLength <= 0)
            return null;

        var value = rangeHeader.Trim();
        if (!value.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return null;

        var spec = value["bytes=".Length..].Trim();
        var dash = spec.IndexOf('-');
        if (dash < 0)
            return null;

        var startStr = spec[..dash].Trim();
        var endStr = spec[(dash + 1)..].Trim();

        if (string.IsNullOrEmpty(startStr) && string.IsNullOrEmpty(endStr))
            return null;

        long start, end;

        if (string.IsNullOrEmpty(startStr))
        {
            // suffix: bytes=-N means last N bytes
            if (!long.TryParse(endStr, out var suffix) || suffix <= 0)
                return null;
            start = Math.Max(0, totalLength - suffix);
            end = totalLength - 1;
        }
        else if (string.IsNullOrEmpty(endStr))
        {
            // open-ended: bytes=start-
            if (!long.TryParse(startStr, out start) || start < 0)
                return null;
            end = totalLength - 1;
        }
        else
        {
            if (!long.TryParse(startStr, out start) || !long.TryParse(endStr, out end))
                return null;
            if (start < 0 || end < start)
                return null;
            if (end >= totalLength)
                end = totalLength - 1;
        }

        if (start >= totalLength)
            return null;

        return (start, end);
    }
}
