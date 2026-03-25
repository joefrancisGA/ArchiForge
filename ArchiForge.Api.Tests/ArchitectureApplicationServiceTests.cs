using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ArchitectureApplicationServiceTests
{
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService;
    private readonly Mock<IArchitectureRunRepository> _runRepository;
    private readonly Mock<IAgentResultRepository> _resultRepository;
    private readonly Mock<IGoldenManifestRepository> _manifestRepository;
    private readonly Mock<IArchitectureRequestRepository> _requestRepository;
    private readonly ArchitectureApplicationService _sut;

    public ArchitectureApplicationServiceTests()
    {
        _runDetailQueryService = new Mock<IRunDetailQueryService>();
        _runRepository = new Mock<IArchitectureRunRepository>();
        _resultRepository = new Mock<IAgentResultRepository>();
        _manifestRepository = new Mock<IGoldenManifestRepository>();
        _requestRepository = new Mock<IArchitectureRequestRepository>();
        Mock<ILogger<ArchitectureApplicationService>> logger = new();

        _sut = new ArchitectureApplicationService(
            _runDetailQueryService.Object,
            _runRepository.Object,
            _resultRepository.Object,
            _manifestRepository.Object,
            _requestRepository.Object,
            logger.Object);
    }

    private static ArchitectureRequest ValidRequest() => new()
    {
        RequestId = "req-1",
        Description = "A system for testing architecture",
        SystemName = "TestSystem",
        Environment = "prod",
        CloudProvider = CloudProvider.Azure
    };

    private static ArchitectureRun ValidRun(string runId = "run-1", string requestId = "req-1") => new()
    {
        RunId = runId,
        RequestId = requestId,
        Status = ArchitectureRunStatus.TasksGenerated,
        CreatedUtc = DateTime.UtcNow
    };

    private static AgentTask ValidTask(string runId = "run-1", AgentType type = AgentType.Topology) => new()
    {
        TaskId = "task-1",
        RunId = runId,
        AgentType = type,
        Objective = "Design topology",
        Status = AgentTaskStatus.Created
    };

    private static AgentResult ValidResult(string runId = "run-1", AgentType type = AgentType.Topology) => new()
    {
        ResultId = "result-1",
        TaskId = "task-1",
        RunId = runId,
        AgentType = type,
        Claims = ["Claim"],
        EvidenceRefs = [],
        Confidence = 0.9
    };

    private static ArchitectureRunDetail DetailFor(
        ArchitectureRun run,
        IReadOnlyList<AgentTask>? tasks = null,
        IReadOnlyList<AgentResult>? results = null)
        => new()
        {
            Run = run,
            Tasks = (tasks ?? []).ToList(),
            Results = (results ?? []).ToList()
        };

    #region GetRunAsync

    [Fact]
    public async Task GetRunAsync_WhenRunExists_ReturnsRunWithTasksAndResults()
    {
        ArchitectureRun run = ValidRun();
        List<AgentTask> tasks = new() { ValidTask() };
        List<AgentResult> results = new() { ValidResult() };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, results));

        GetRunResult? result = await _sut.GetRunAsync("run-1");

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be("run-1");
        result.Tasks.Should().HaveCount(1);
        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRunAsync_WhenRunNotFound_ReturnsNull()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        GetRunResult? result = await _sut.GetRunAsync("nonexistent");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetRunAsync_WhenRunIdIsNullOrWhiteSpace_ReturnsNull(string? runId)
    {
        GetRunResult? result = await _sut.GetRunAsync(runId!);

        result.Should().BeNull();
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunAsync_WhenRunHasNoTasksOrResults_ReturnsEmptyCollections()
    {
        ArchitectureRun run = ValidRun();
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [], []));

        GetRunResult? result = await _sut.GetRunAsync("run-1");

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be("run-1");
        result.Tasks.Should().BeEmpty();
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunAsync_PassesCancellationTokenToRepositories()
    {
        ArchitectureRun run = ValidRun();
        CancellationTokenSource cts = new();
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", cts.Token))
            .ReturnsAsync(DetailFor(run, [], []));

        await _sut.GetRunAsync("run-1", cts.Token);

        _runDetailQueryService.Verify(s => s.GetRunDetailAsync("run-1", cts.Token), Times.Once);
    }

    #endregion

    #region SubmitAgentResultAsync

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunExists_StoresResultAndUpdatesStatus()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        List<AgentTask> tasks = new() { ValidTask() };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        sutResult.ResultId.Should().Be("result-1");
        sutResult.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SubmitAgentResultAsync_WhenRunIdIsNullOrWhiteSpace_ReturnsError(string? runId)
    {
        SubmitResultResult result = await _sut.SubmitAgentResultAsync(runId!, ValidResult());

        result.Success.Should().BeFalse();
        result.Error.Should().Be("RunId is required.");
        _runRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenResultIsNull_ReturnsError()
    {
        SubmitResultResult result = await _sut.SubmitAgentResultAsync("run-1", null!);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Agent result is required.");
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenThreeResultsWithDistinctRequiredTypes_TransitionsToReadyForCommit()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult("run-1", AgentType.Compliance);
        result.TaskId = "task-compliance";
        List<AgentTask> tasks = new()
        {
            ValidTask(),
            ValidTask("run-1", AgentType.Cost),
            ValidTask("run-1", AgentType.Compliance)
        };
        tasks[0].TaskId = "task-topology";
        tasks[1].TaskId = "task-cost";
        tasks[2].TaskId = "task-compliance";
        List<AgentResult> existingResults = new()
        {
            ValidResult(),
            ValidResult("run-1", AgentType.Cost)
        };
        existingResults[0].TaskId = "task-topology";
        existingResults[1].TaskId = "task-cost";

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _runRepository.Verify(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenThreeResultsButSameAgentType_StaysWaitingForResults()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        result.TaskId = "task-topology-3";
        List<AgentTask> tasks = new()
        {
            ValidTask(),
            ValidTask(),
            ValidTask()
        };
        tasks[0].TaskId = "task-topology-1";
        tasks[1].TaskId = "task-topology-2";
        tasks[2].TaskId = "task-topology-3";
        List<AgentResult> existingResults = new()
        {
            ValidResult(),
            ValidResult()
        };
        existingResults[0].TaskId = "task-topology-1";
        existingResults[1].TaskId = "task-topology-2";

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _runRepository.Verify(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _runRepository.Verify(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunNotFound_ReturnsError()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);
        AgentResult result = ValidResult("nonexistent");

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("nonexistent", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunStatusDoesNotAllowSubmission_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        run.Status = ArchitectureRunStatus.ReadyForCommit;
        AgentResult result = ValidResult();

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [ValidTask()], []));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not accept agent results");
        sutResult.Error.Should().Contain("TasksGenerated");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunIdMismatch_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult("run-2");
        result.RunId = "run-2";

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [ValidTask()], []));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not match");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunIdMatchIsCaseInsensitive_AcceptsResult()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        result.RunId = "RUN-1";
        List<AgentTask> tasks = new() { ValidTask() };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _resultRepository.Setup(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _resultRepository.Verify(r => r.CreateAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenResultForTaskAlreadySubmitted_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        List<AgentTask> tasks = new() { ValidTask() };
        List<AgentResult> existingResults = new() { result };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("already been submitted");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenTaskNotFoundForRun_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        result.TaskId = "nonexistent-task";
        List<AgentTask> tasks = new() { ValidTask() };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("was not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenResultAgentTypeDoesNotMatchTask_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult("run-1", AgentType.Cost);
        result.TaskId = "task-1";
        List<AgentTask> tasks = new() { ValidTask() };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not match task AgentType");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetManifestAsync

    [Fact]
    public async Task GetManifestAsync_WhenVersionExists_ReturnsManifest()
    {
        GoldenManifest manifest = new() { RunId = "run-1", SystemName = "TestSystem", Metadata = new ManifestMetadata { ManifestVersion = "v1" } };
        _manifestRepository.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(manifest);

        GoldenManifest? result = await _sut.GetManifestAsync("v1");

        result.Should().NotBeNull();
        result.Metadata.ManifestVersion.Should().Be("v1");
    }

    [Fact]
    public async Task GetManifestAsync_WhenVersionNotFound_ReturnsNull()
    {
        _manifestRepository.Setup(r => r.GetByVersionAsync("nonexistent", It.IsAny<CancellationToken>())).ReturnsAsync((GoldenManifest?)null);

        GoldenManifest? result = await _sut.GetManifestAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetManifestAsync_PassesCancellationTokenToRepository()
    {
        CancellationTokenSource cts = new();
        GoldenManifest manifest = new() { Metadata = new ManifestMetadata { ManifestVersion = "v1" } };
        _manifestRepository.Setup(r => r.GetByVersionAsync("v1", cts.Token)).ReturnsAsync(manifest);

        await _sut.GetManifestAsync("v1", cts.Token);

        _manifestRepository.Verify(r => r.GetByVersionAsync("v1", cts.Token), Times.Once);
    }

    #endregion

    #region SeedFakeResultsAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SeedFakeResultsAsync_WhenRunIdIsNullOrWhiteSpace_ReturnsError(string? runId)
    {
        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync(runId!);

        result.Success.Should().BeFalse();
        result.ResultCount.Should().Be(0);
        result.Error.Should().Be("RunId is required.");
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenValid_SeedsResultsAndUpdatesStatus()
    {
        ArchitectureRun run = ValidRun();
        ArchitectureRequest request = ValidRequest();
        List<AgentTask> tasks = new() { ValidTask(), ValidTask("run-1", AgentType.Cost), ValidTask("run-1", AgentType.Compliance) };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _resultRepository.Setup(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(3);
        result.Error.Should().BeNull();
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRunStatusNotAllowed_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        run.Status = ArchitectureRunStatus.ReadyForCommit;
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [ValidTask()], []));

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("does not accept results").And.Contain("ReadyForCommit");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
        _runRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ArchitectureRunStatus>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRunNotFound_ReturnsError()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("nonexistent");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRequestNotFound_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [ValidTask()], []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRequest?)null);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenNoTasks_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        ArchitectureRequest request = ValidRequest();
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [], []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No tasks");
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRunAlreadyHasResults_ReturnsSuccessWithZeroCountAndDoesNotCreate()
    {
        ArchitectureRun run = ValidRun();
        ArchitectureRequest request = ValidRequest();
        List<AgentTask> tasks = new() { ValidTask() };
        List<AgentResult> existingResults = new() { ValidResult() };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(0);
        result.Error.Should().BeNull();
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
        _runRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ArchitectureRunStatus>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
