using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Application.Jobs;
using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class JobsControllerTests
{
    private static JobsController CreateSut(Mock<IBackgroundJobQueue> jobs)
    {
        JobsController controller = new(jobs.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        return controller;
    }

    [SkippableFact]
    public async Task GetJob_whitespace_jobId_returns_400()
    {
        Mock<IBackgroundJobQueue> jobs = new();
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.GetJob("   ", CancellationToken.None);

        ObjectResult obj = result.Should().BeAssignableTo<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        jobs.Verify(j => j.GetInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [SkippableFact]
    public async Task GetJob_unknown_returns_404()
    {
        Mock<IBackgroundJobQueue> jobs = new();
        jobs
            .Setup(j => j.GetInfoAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BackgroundJobInfo?)null);
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.GetJob("missing", CancellationToken.None);

        ObjectResult obj = result.Should().BeAssignableTo<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [SkippableFact]
    public async Task GetJob_found_returns_200()
    {
        BackgroundJobInfo info = new(
            "j1",
            BackgroundJobState.Succeeded,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            "out.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        Mock<IBackgroundJobQueue> jobs = new();
        jobs.Setup(j => j.GetInfoAsync("j1", It.IsAny<CancellationToken>())).ReturnsAsync(info);
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.GetJob("j1", CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(info);
    }

    [SkippableFact]
    public async Task DownloadJobFile_whitespace_jobId_returns_400()
    {
        Mock<IBackgroundJobQueue> jobs = new();
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.DownloadJobFile(" ", CancellationToken.None);

        ObjectResult obj = result.Should().BeAssignableTo<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        jobs.Verify(j => j.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [SkippableFact]
    public async Task DownloadJobFile_unknown_returns_404()
    {
        Mock<IBackgroundJobQueue> jobs = new();
        jobs
            .Setup(j => j.GetInfoAsync("x", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BackgroundJobInfo?)null);
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.DownloadJobFile("x", CancellationToken.None);

        ObjectResult obj = result.Should().BeAssignableTo<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [SkippableFact]
    public async Task DownloadJobFile_not_succeeded_returns_409()
    {
        BackgroundJobInfo info = new(
            "j1",
            BackgroundJobState.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null);
        Mock<IBackgroundJobQueue> jobs = new();
        jobs.Setup(j => j.GetInfoAsync("j1", It.IsAny<CancellationToken>())).ReturnsAsync(info);
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.DownloadJobFile("j1", CancellationToken.None);

        ObjectResult obj = result.Should().BeAssignableTo<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        jobs.Verify(j => j.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [SkippableFact]
    public async Task DownloadJobFile_succeeded_without_file_returns_409()
    {
        BackgroundJobInfo info = new(
            "j1",
            BackgroundJobState.Succeeded,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null,
            null);
        Mock<IBackgroundJobQueue> jobs = new();
        jobs.Setup(j => j.GetInfoAsync("j1", It.IsAny<CancellationToken>())).ReturnsAsync(info);
        jobs
            .Setup(j => j.GetFileAsync("j1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BackgroundJobFile?)null);
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.DownloadJobFile("j1", CancellationToken.None);

        ObjectResult obj = result.Should().BeAssignableTo<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [SkippableFact]
    public async Task DownloadJobFile_succeeded_with_file_returns_file()
    {
        BackgroundJobInfo info = new(
            "j1",
            BackgroundJobState.Succeeded,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            "r.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        BackgroundJobFile file = new("r.docx", "application/octet-stream", [0x01, 0x02]);
        Mock<IBackgroundJobQueue> jobs = new();
        jobs.Setup(j => j.GetInfoAsync("j1", It.IsAny<CancellationToken>())).ReturnsAsync(info);
        jobs.Setup(j => j.GetFileAsync("j1", It.IsAny<CancellationToken>())).ReturnsAsync(file);
        JobsController sut = CreateSut(jobs);

        IActionResult result = await sut.DownloadJobFile("j1", CancellationToken.None);

        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().Equal(file.Bytes);
        fileResult.ContentType.Should().Be(file.ContentType);
        fileResult.FileDownloadName.Should().Be(file.FileName);
    }
}
