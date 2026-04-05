using ArchiForge.Application.Jobs;

using FluentAssertions;

namespace ArchiForge.Application.Tests.Jobs;

[Trait("Category", "Unit")]
public sealed class BackgroundJobWorkUnitJsonTests
{
    [Fact]
    public void RoundTrip_AnalysisReportDocxWorkUnit_PreservesPayload()
    {
        AnalysisReportDocxWorkUnit original = new(
            new AnalysisReportDocxJobPayload { RunId = "run-1", IncludeDiagram = false },
            "report.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        string json = BackgroundJobWorkUnitJson.Serialize(original);
        BackgroundJobWorkUnit? restored = BackgroundJobWorkUnitJson.Deserialize(json);

        restored.Should().BeOfType<AnalysisReportDocxWorkUnit>();
        AnalysisReportDocxWorkUnit typed = (AnalysisReportDocxWorkUnit)restored;
        typed.Payload.RunId.Should().Be("run-1");
        typed.Payload.IncludeDiagram.Should().BeFalse();
        typed.FileName.Should().Be("report.docx");
        typed.ContentType.Should().Contain("wordprocessingml");
    }

    [Fact]
    public void RoundTrip_ConsultingDocxWorkUnit_PreservesPayload()
    {
        ConsultingDocxWorkUnit original = new(
            new ConsultingDocxJobPayload { RunId = "run-2", TemplateProfile = "exec" },
            "c.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        string json = BackgroundJobWorkUnitJson.Serialize(original);
        BackgroundJobWorkUnit? restored = BackgroundJobWorkUnitJson.Deserialize(json);

        restored.Should().BeOfType<ConsultingDocxWorkUnit>();
        ConsultingDocxWorkUnit typed = (ConsultingDocxWorkUnit)restored;
        typed.Payload.RunId.Should().Be("run-2");
        typed.Payload.TemplateProfile.Should().Be("exec");
    }
}
