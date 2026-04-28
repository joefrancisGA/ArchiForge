namespace ArchLucid.Persistence.Tests.Value;

/// <summary>
///     Guard-rail: review-cycle SQL stays aligned with golden-manifest window scoping (no LocalDB harness in this
///     assembly).
/// </summary>
[Trait("Category", "Unit")]
public sealed class DapperValueReportMetricsReaderReviewCycleTests
{
    [Fact]
    public void DapperValueReportMetricsReader_source_contains_join_and_avg_hours_expression()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string path = Path.Combine(root, "ArchLucid.Persistence", "Value", "DapperValueReportMetricsReader.cs");
        File.Exists(path).Should().BeTrue("expected {0}", path);

        string source = File.ReadAllText(path);

        source.Should().Contain("INNER JOIN dbo.Runs r ON m.RunId = r.RunId");
        source.Should().Contain("DATEDIFF(SECOND, r.CreatedUtc, m.CreatedUtc)");
        source.Should().Contain("BaselineReviewCycleHours");
    }
}
