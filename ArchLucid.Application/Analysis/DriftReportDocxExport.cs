namespace ArchLucid.Application.Analysis;

/// <summary>
///     Generates a Docx-format comparison drift report from a <see cref="DriftAnalysisResult" />.
/// </summary>
/// <remarks>
///     Uses <see cref="OpenXmlDocxDocumentBuilder" /> to produce a structured document with a
///     header, drift summary, and a differences table where applicable.
/// </remarks>
public sealed class DriftReportDocxExport : IDriftReportDocxExport
{
    /// <inheritdoc />
    public byte[] GenerateDocx(DriftAnalysisResult drift, string? comparisonRecordId = null)
    {
        ArgumentNullException.ThrowIfNull(drift);

        using OpenXmlDocxDocumentBuilder builder = new();
        builder.AddHeading("ArchLucid Comparison Drift Report", 1);
        if (!string.IsNullOrWhiteSpace(comparisonRecordId))
        {
            builder.AddParagraph($"Comparison record: {comparisonRecordId}");
            builder.AddSpacer();
        }

        builder.AddParagraph($"Drift detected: {(drift.DriftDetected ? "Yes" : "No")}");
        if (!string.IsNullOrWhiteSpace(drift.Summary))
            builder.AddParagraph(drift.Summary);
        builder.AddSpacer();
        if (drift.Items.Count <= 0)
            return builder.Build();
        builder.AddHeading("Differences", 2);

        foreach (DriftItem item in drift.Items)
        {
            builder.AddParagraph($"{item.Category} — {item.Path}", true);
            if (!string.IsNullOrEmpty(item.Description))
                builder.AddParagraph(item.Description);

            if (item.StoredValue is null && item.RegeneratedValue is null)
                continue;

            if (item.StoredValue is not null)
                builder.AddBullet($"Stored: {item.StoredValue}");
            if (item.RegeneratedValue is not null)
                builder.AddBullet($"Regenerated: {item.RegeneratedValue}");
        }

        builder.AddSpacer();
        return builder.Build();
    }
}
