namespace ArchiForge.Application.Diffs;

public interface IManifestDiffSummaryFormatter
{
    string FormatMarkdown(ManifestDiffResult diff);
}
