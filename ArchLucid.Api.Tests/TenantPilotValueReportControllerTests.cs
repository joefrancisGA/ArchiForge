using ArchLucid.Api.Controllers.Tenancy;
using ArchLucid.Application.Pilots;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TenantPilotValueReportControllerTests
{
    [SkippableFact]
    public async Task GetPilotValueReport_returns_json_by_default()
    {
        Mock<IPilotValueReportService> svc = new();
        PilotValueReport body = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            FromUtc = DateTime.UtcNow.AddDays(-7),
            ToUtc = DateTime.UtcNow,
            TotalRunsCommitted = 2,
            GovernancePendingApprovalsNow = 0
        };

        svc.Setup(s => s.BuildAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(body);

        TenantPilotValueReportController sut = new(svc.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.GetPilotValueReport(null, null, CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(body);
    }

    [SkippableFact]
    public async Task GetPilotValueReport_returns_problem_details_when_tenant_missing()
    {
        Mock<IPilotValueReportService> svc = new();
        svc.Setup(s => s.BuildAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PilotValueReport?)null);

        TenantPilotValueReportController sut = new(svc.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.GetPilotValueReport(null, null, CancellationToken.None);

        ObjectResult problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        Microsoft.AspNetCore.Mvc.ProblemDetails? pd = problem.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        pd.Should().NotBeNull();
        pd.Detail.Should().Be("Tenant was not found for the current scope.");
    }

    [SkippableFact]
    public async Task GetPilotValueReport_returns_markdown_when_accept_contains_text_markdown()
    {
        Mock<IPilotValueReportService> svc = new();
        DateTime fromUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);
        PilotValueReport body = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            FromUtc = fromUtc,
            ToUtc = toUtc,
            TotalRunsCommitted = 1,
            TotalFindings = 3,
            FindingsBySeverity = new PilotValueReportSeverityBreakdown { Critical = 1, High = 1, Medium = 1 },
            UniqueAgentTypes = ["Topology"],
            CommittedRunsTimeline =
            [
                new PilotValueReportRunTimelinePoint
                {
                    RunId = "run", CreatedUtc = fromUtc, CommittedUtc = fromUtc.AddHours(1), SystemName = "sys"
                }
            ]
        };

        svc.Setup(s => s.BuildAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(body);

        DefaultHttpContext http = new();
        http.Request.Headers.Accept = "text/markdown";

        TenantPilotValueReportController sut = new(svc.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        IActionResult result = await sut.GetPilotValueReport(null, null, CancellationToken.None);

        ContentResult content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Contain("text/markdown");
        content.Content.Should().Contain("# ArchLucid pilot value report");
        content.Content.Should().Contain("| Critical | 1 |");
        content.Content.Should().Contain("| Committed runs | 1 |");
    }
}
