using ArchLucid.Contracts.ValueReports;

namespace ArchLucid.ArtifactSynthesis.Docx;

public interface IValueReportRenderer
{
    Task<byte[]> RenderAsync(ValueReportSnapshot snapshot, CancellationToken cancellationToken);
}
