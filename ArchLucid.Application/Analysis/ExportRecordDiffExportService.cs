namespace ArchLucid.Application.Analysis;

public sealed class ExportRecordDiffExportService : IExportRecordDiffExportService
{
    public Task<byte[]> GenerateDocxAsync(
        ExportRecordDiffResult diff,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diff);

        using OpenXmlDocxDocumentBuilder builder = new();

        builder.AddHeading("ArchLucid Export Record Diff Comparison Export", 1);

        builder.AddParagraph($"Left Export Record ID: {diff.LeftExportRecordId}");
        builder.AddParagraph($"Right Export Record ID: {diff.RightExportRecordId}");
        builder.AddParagraph($"Left Run ID: {diff.LeftRunId}");
        builder.AddParagraph($"Right Run ID: {diff.RightRunId}");

        builder.AddSpacer();

        if (diff.ChangedTopLevelFields.Count > 0)
        {
            builder.AddHeading("Changed Top-Level Fields", 2);
            foreach (string item in diff.ChangedTopLevelFields)

                builder.AddBullet(item);

            builder.AddSpacer();
        }

        if (diff.RequestDiff.ChangedFlags.Count > 0 || diff.RequestDiff.ChangedValues.Count > 0)
        {
            builder.AddHeading("Request Diff", 2);

            if (diff.RequestDiff.ChangedFlags.Count > 0)
            {
                builder.AddHeading("Changed Flags", 3);
                foreach (string item in diff.RequestDiff.ChangedFlags)

                    builder.AddBullet(item);
            }

            if (diff.RequestDiff.ChangedValues.Count > 0)
            {
                builder.AddHeading("Changed Values", 3);
                foreach (string item in diff.RequestDiff.ChangedValues)

                    builder.AddBullet(item);
            }

            builder.AddSpacer();
        }

        if (diff.Warnings.Count <= 0)
            return Task.FromResult(builder.Build());

        builder.AddHeading("Warnings", 2);
        foreach (string item in diff.Warnings)

            builder.AddBullet(item);

        builder.AddSpacer();

        return Task.FromResult(builder.Build());
    }
}
