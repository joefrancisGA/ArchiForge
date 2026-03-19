using ArchiForge.Application.Analysis;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Services;

/// <summary>
/// Centralizes file responses for export replay and comparison replay artifacts (markdown/docx/html/pdf).
/// </summary>
public static class ReplayArtifactResponseFactory
{
    public static IActionResult FromExportReplay(HttpRequest request, ReplayExportResult result)
    {
        if (string.Equals(result.Format, "markdown", StringComparison.OrdinalIgnoreCase))
            return ApiFileResults.RangeBytes(request, result.Content, "text/markdown", result.FileName);

        if (string.Equals(result.Format, "docx", StringComparison.OrdinalIgnoreCase))
        {
            return ApiFileResults.RangeBytes(
                request,
                result.Content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                result.FileName);
        }

        throw new InvalidOperationException($"Unsupported replay format '{result.Format}'.");
    }

    /// <summary>Returns a ranged file response for a comparison replay result, or null if format is unsupported.</summary>
    public static IActionResult? TryComparisonReplayFile(HttpRequest request, ReplayComparisonResult result)
    {
        if (string.Equals(result.Format, "markdown", StringComparison.OrdinalIgnoreCase))
            return ApiFileResults.RangeText(request, result.Content, "text/markdown", result.FileName);

        if (string.Equals(result.Format, "html", StringComparison.OrdinalIgnoreCase))
            return ApiFileResults.RangeText(request, result.Content, "text/html", result.FileName);

        if (string.Equals(result.Format, "docx", StringComparison.OrdinalIgnoreCase))
        {
            return ApiFileResults.RangeBytes(
                request,
                result.BinaryContent,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                result.FileName);
        }

        if (string.Equals(result.Format, "pdf", StringComparison.OrdinalIgnoreCase))
            return ApiFileResults.RangeBytes(request, result.BinaryContent, "application/pdf", result.FileName);

        return null;
    }

    public static IActionResult ComparisonReplayFileOrBadRequest(
        HttpRequest request,
        ReplayComparisonResult result,
        Func<IActionResult> badRequest)
    {
        return TryComparisonReplayFile(request, result) ?? badRequest();
    }

    /// <summary>Payload bytes for ZIP batch packaging (text vs binary formats).</summary>
    public static byte[] GetComparisonReplayEntryBytes(ReplayComparisonResult result)
    {
        if (string.Equals(result.Format, "markdown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(result.Format, "html", StringComparison.OrdinalIgnoreCase))
            return System.Text.Encoding.UTF8.GetBytes(result.Content ?? string.Empty);

        return result.BinaryContent ?? [];
    }
}
