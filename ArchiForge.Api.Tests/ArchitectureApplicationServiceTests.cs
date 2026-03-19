using ArchiForge.Api.Services;
using ArchiForge.Contracts.Agents;
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
    private readonly Mock<IArchitectureRunRepository> _runRepository;
    private readonly Mock<IAgentTaskRepository> _taskRepository;
    private readonly Mock<IAgentResultRepository> _resultRepository;
    private readonly Mock<IGoldenManifestRepository> _manifestRepository;
    private readonly Mock<IArchitectureRequestRepository> _requestRepository;
    private readonly ArchitectureApplicationService _sut;

    public ArchitectureApplicationServiceTests()
    {
        _runRepository = new Mock<IArchitectureRunRepository>();
        _taskRepository = new Mock<IAgentTaskRepository>();
        _resultRepository = new Mock<IAgentResultRepository>();
        _manifestRepository = new Mock<IGoldenManifestRepository>();
        _requestRepository = new Mock<IArchitectureRequestRepository>();
        var logger = new Mock<ILogger<ArchitectureApplicationService>>();

        _sut = new ArchitectureApplicationService(
            _runRepository.Object,
            _taskRepository.Object,
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

    #region GetRunAsync

    [Fact]
    public async Task GetRunAsync_WhenRunExists_ReturnsRunWithTasksAndResults()
    {
        var run = ValidRun();
        var tasks = new List<AgentTask> { ValidTask() };
        var results = new List<AgentResult> { ValidResult() };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(results);

        var result = await _sut.GetRunAsync("run-1");

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be("run-1");
        result.Tasks.Should().HaveCount(1);
        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRunAsync_WhenRunNotFound_ReturnsNull()
    {
        _runRepository.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRun?)null);

        var result = await _sut.GetRunAsync("nonexistent");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetRunAsync_WhenRunIdIsNullOrWhiteSpace_ReturnsNull(string? runId)
    {
        var result = await _sut.GetRunAsync(runId!);

        result.Should().BeNull();
        _runRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunAsync_WhenRunHasNoTasksOrResults_ReturnsEmptyCollections()
    {
        var run = ValidRun();
        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentTask>());
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult>());

        var result = await _sut.GetRunAsync("run-1");

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be("run-1");
        result.Tasks.Should().BeEmpty();
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunAsync_PassesCancellationTokenToRepositories()
    {
        var run = ValidRun();
        var cts = new CancellationTokenSource();
        _runRepository.Setup(r => r.GetByIdAsync("run-1", cts.Token)).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", cts.Token)).ReturnsAsync(new List<AgentTask>());
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", cts.Token)).ReturnsAsync(new List<AgentResult>());

        await _sut.GetRunAsync("run-1", cts.Token);

        _runRepository.Verify(r => r.GetByIdAsync("run-1", cts.Token), Times.Once);
        _taskRepository.Verify(r => r.GetByRunIdAsync("run-1", cts.Token), Times.Once);
        _resultRepository.Verify(r => r.GetByRunIdAsync("run-1", cts.Token), Times.Once);
    }

    #endregion

    #region SubmitAgentResultAsync

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunExists_StoresResultAndUpdatesStatus()
    {
        var run = ValidRun();
        var result = ValidResult();
        var tasks = new List<AgentTask> { ValidTask() };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult>());
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

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
        var result = await _sut.SubmitAgentResultAsync(runId!, ValidResult());

        result.Success.Should().BeFalse();
        result.Error.Should().Be("RunId is required.");
        _runRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenResultIsNull_ReturnsError()
    {
        var result = await _sut.SubmitAgentResultAsync("run-1", null!);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Agent result is required.");
        _runRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenThreeResultsWithDistinctRequiredTypes_TransitionsToReadyForCommit()
    {
        var run = ValidRun();
        var result = ValidResult("run-1", AgentType.Compliance);
        result.TaskId = "task-compliance";
        var tasks = new List<AgentTask>
        {
            ValidTask(),
            ValidTask("run-1", AgentType.Cost),
            ValidTask("run-1", AgentType.Compliance)
        };
        tasks[0].TaskId = "task-topology";
        tasks[1].TaskId = "task-cost";
        tasks[2].TaskId = "task-compliance";
        var existingResults = new List<AgentResult>
        {
            ValidResult(),
            ValidResult("run-1", AgentType.Cost)
        };
        existingResults[0].TaskId = "task-topology";
        existingResults[1].TaskId = "task-cost";

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResults);
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _runRepository.Verify(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenThreeResultsButSameAgentType_StaysWaitingForResults()
    {
        var run = ValidRun();
        var result = ValidResult();
        result.TaskId = "task-topology-3";
        var tasks = new List<AgentTask>
        {
            ValidTask(),
            ValidTask(),
            ValidTask()
        };
        tasks[0].TaskId = "task-topology-1";
        tasks[1].TaskId = "task-topology-2";
        tasks[2].TaskId = "task-topology-3";
        var existingResults = new List<AgentResult>
        {
            ValidResult(),
            ValidResult()
        };
        existingResults[0].TaskId = "task-topology-1";
        existingResults[1].TaskId = "task-topology-2";

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResults);
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _runRepository.Verify(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _runRepository.Verify(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunNotFound_ReturnsError()
    {
        _runRepository.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRun?)null);
        var result = ValidResult("nonexistent");

        var sutResult = await _sut.SubmitAgentResultAsync("nonexistent", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunStatusDoesNotAllowSubmission_ReturnsError()
    {
        var run = ValidRun();
        run.Status = ArchitectureRunStatus.ReadyForCommit;
        var result = ValidResult();

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not accept agent results");
        sutResult.Error.Should().Contain("TasksGenerated");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunIdMismatch_ReturnsError()
    {
        var run = ValidRun();
        var result = ValidResult("run-2");
        result.RunId = "run-2";

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not match");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenRunIdMatchIsCaseInsensitive_AcceptsResult()
    {
        var run = ValidRun();
        var result = ValidResult();
        result.RunId = "RUN-1";
        var tasks = new List<AgentTask> { ValidTask() };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult>());
        _resultRepository.Setup(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.WaitingForResults, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _resultRepository.Verify(r => r.CreateAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenResultForTaskAlreadySubmitted_ReturnsError()
    {
        var run = ValidRun();
        var result = ValidResult();
        var tasks = new List<AgentTask> { ValidTask() };
        var existingResults = new List<AgentResult> { result };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResults);

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("already been submitted");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenTaskNotFoundForRun_ReturnsError()
    {
        var run = ValidRun();
        var result = ValidResult();
        result.TaskId = "nonexistent-task";
        var tasks = new List<AgentTask> { ValidTask() };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult>());

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("was not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAgentResultAsync_WhenResultAgentTypeDoesNotMatchTask_ReturnsError()
    {
        var run = ValidRun();
        var result = ValidResult("run-1", AgentType.Cost);
        result.TaskId = "task-1";
        var tasks = new List<AgentTask> { ValidTask() };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult>());

        var sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not match task AgentType");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetManifestAsync

    [Fact]
    public async Task GetManifestAsync_WhenVersionExists_ReturnsManifest()
    {
        var manifest = new GoldenManifest { RunId = "run-1", SystemName = "TestSystem", Metadata = new ManifestMetadata { ManifestVersion = "v1" } };
        _manifestRepository.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(manifest);

        var result = await _sut.GetManifestAsync("v1");

        result.Should().NotBeNull();
        result.Metadata.ManifestVersion.Should().Be("v1");
    }

    [Fact]
    public async Task GetManifestAsync_WhenVersionNotFound_ReturnsNull()
    {
        _manifestRepository.Setup(r => r.GetByVersionAsync("nonexistent", It.IsAny<CancellationToken>())).ReturnsAsync((GoldenManifest?)null);

        var result = await _sut.GetManifestAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetManifestAsync_PassesCancellationTokenToRepository()
    {
        var cts = new CancellationTokenSource();
        var manifest = new GoldenManifest { Metadata = new ManifestMetadata { ManifestVersion = "v1" } };
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
        var result = await _sut.SeedFakeResultsAsync(runId!);

        result.Success.Should().BeFalse();
        result.ResultCount.Should().Be(0);
        result.Error.Should().Be("RunId is required.");
        _runRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenValid_SeedsResultsAndUpdatesStatus()
    {
        var run = ValidRun();
        var request = ValidRequest();
        var tasks = new List<AgentTask> { ValidTask(), ValidTask("run-1", AgentType.Cost), ValidTask("run-1", AgentType.Compliance) };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _runRepository.Setup(r => r.UpdateStatusAsync("run-1", ArchitectureRunStatus.ReadyForCommit, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(3);
        result.Error.Should().BeNull();
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRunStatusNotAllowed_ReturnsError()
    {
        var run = ValidRun();
        run.Status = ArchitectureRunStatus.ReadyForCommit;
        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("does not accept results").And.Contain("ReadyForCommit");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
        _runRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ArchitectureRunStatus>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRunNotFound_ReturnsError()
    {
        _runRepository.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRun?)null);

        var result = await _sut.SeedFakeResultsAsync("nonexistent");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRequestNotFound_ReturnsError()
    {
        var run = ValidRun();
        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRequest?)null);

        var result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenNoTasks_ReturnsError()
    {
        var run = ValidRun();
        var request = ValidRequest();
        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentTask>());

        var result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No tasks");
    }

    [Fact]
    public async Task SeedFakeResultsAsync_WhenRunAlreadyHasResults_ReturnsSuccessWithZeroCountAndDoesNotCreate()
    {
        var run = ValidRun();
        var request = ValidRequest();
        var tasks = new List<AgentTask> { ValidTask() };
        var existingResults = new List<AgentResult> { ValidResult() };

        _runRepository.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _taskRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResults);

        var result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(0);
        result.Error.Should().BeNull();
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()), Times.Never);
        _runRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ArchitectureRunStatus>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
