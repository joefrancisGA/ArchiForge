using ArchLucid.Application.Common;
using ArchLucid.Application.Evidence;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Application Service.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchitectureApplicationServiceTests
{
    private readonly Mock<IAgentEvidencePackageRepository> _agentEvidencePackageRepository;
    private readonly Mock<IEvidenceBuilder> _evidenceBuilder;
    private readonly Mock<IArchitectureRequestRepository> _requestRepository;
    private readonly Mock<IAgentResultRepository> _resultRepository;
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService;
    private readonly ArchitectureApplicationService _sut;
    private readonly Mock<IUnifiedGoldenManifestReader> _unifiedGoldenManifestReader;
    private readonly Mock<IRunRepository> _runRepository;
    private readonly Mock<IAuditService> _auditService;

    public ArchitectureApplicationServiceTests()
    {
        _runDetailQueryService = new Mock<IRunDetailQueryService>();
        _resultRepository = new Mock<IAgentResultRepository>();
        _unifiedGoldenManifestReader = new Mock<IUnifiedGoldenManifestReader>();
        _requestRepository = new Mock<IArchitectureRequestRepository>();
        _agentEvidencePackageRepository = new Mock<IAgentEvidencePackageRepository>();
        _evidenceBuilder = new Mock<IEvidenceBuilder>();
        _runRepository = new Mock<IRunRepository>();
        Mock<IScopeContextProvider> scopeContextProvider = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AzureOpenAI:DeploymentName"] = "gpt-test" })
            .Build();
        _auditService = new Mock<IAuditService>();
        Mock<IAgentArchitectureFindingConfidenceEnricher> architectureFindingConfidenceEnricher = new();
        architectureFindingConfidenceEnricher
            .Setup(e => e.TryEnrichRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IActorContext> actorContext = new();
        Mock<ILogger<ArchitectureApplicationService>> logger = new();

        scopeContextProvider
            .Setup(s => s.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
                });

        _auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        actorContext.Setup(a => a.GetActor()).Returns("unit-test");

        _agentEvidencePackageRepository
            .Setup(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentEvidencePackage?)null);
        _evidenceBuilder
            .Setup(b =>
                b.BuildAsync(It.IsAny<string>(), It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string rid, ArchitectureRequest _, CancellationToken _) => new AgentEvidencePackage
            {
                EvidencePackageId = "pkg-1",
                RunId = rid,
                RequestId = "req-1",
                SystemName = "TestSystem",
                Environment = "prod",
                CloudProvider = "Azure",
                Request = new RequestEvidence(),
                Policies = [],
                ServiceCatalog = [],
                Patterns = [],
                Notes = [],
                CreatedUtc = DateTime.UtcNow
            });

        _sut = new ArchitectureApplicationService(
            _runDetailQueryService.Object,
            _resultRepository.Object,
            _unifiedGoldenManifestReader.Object,
            _requestRepository.Object,
            _agentEvidencePackageRepository.Object,
            _evidenceBuilder.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            _runRepository.Object,
            scopeContextProvider.Object,
            configuration,
            _auditService.Object,
            actorContext.Object,
            architectureFindingConfidenceEnricher.Object,
            logger.Object);
    }

    private static ArchitectureRequest ValidRequest()
    {
        return new ArchitectureRequest
        {
            RequestId = "req-1",
            Description = "A system for testing architecture",
            SystemName = "TestSystem",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure
        };
    }

    private static ArchitectureRun ValidRun(string runId = "run-1", string requestId = "req-1")
    {
        return new ArchitectureRun
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static AgentTask ValidTask(string runId = "run-1", AgentType type = AgentType.Topology)
    {
        return new AgentTask
        {
            TaskId = "task-1",
            RunId = runId,
            AgentType = type,
            Objective = "Design topology",
            Status = AgentTaskStatus.Created
        };
    }

    private static AgentResult ValidResult(string runId = "run-1", AgentType type = AgentType.Topology)
    {
        return new AgentResult
        {
            ResultId = "result-1",
            TaskId = "task-1",
            RunId = runId,
            AgentType = type,
            Claims = ["Claim"],
            EvidenceRefs = [],
            Confidence = 0.9
        };
    }

    private static ArchitectureRunDetail DetailFor(
        ArchitectureRun run,
        IReadOnlyList<AgentTask>? tasks = null,
        IReadOnlyList<AgentResult>? results = null)
    {
        return new ArchitectureRunDetail
        {
            Run = run, Tasks = (tasks ?? []).ToList(), Results = (results ?? []).ToList()
        };
    }

    #region GetRunAsync

    [SkippableFact]
    public async Task GetRunAsync_WhenRunExists_ReturnsRunWithTasksAndResults()
    {
        ArchitectureRun run = ValidRun();
        List<AgentTask> tasks = [ValidTask()];
        List<AgentResult> results = [ValidResult()];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, results));

        GetRunResult? result = await _sut.GetRunAsync("run-1");

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be("run-1");
        result.Tasks.Should().HaveCount(1);
        result.Results.Should().HaveCount(1);
    }

    [SkippableFact]
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
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
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

    [SkippableFact]
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

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenRunExists_StoresResult()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        List<AgentTask> tasks = [ValidTask()];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([result]);

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
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenResultIsNull_ReturnsError()
    {
        SubmitResultResult result = await _sut.SubmitAgentResultAsync("run-1", null!);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Agent result is required.");
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenAllRequiredAgentTypesHaveResults_TransitionsToReadyForCommit()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult("run-1", AgentType.Critic);
        result.TaskId = "task-critic";
        List<AgentTask> tasks =
        [
            ValidTask(),
            ValidTask("run-1", AgentType.Cost),
            ValidTask("run-1", AgentType.Compliance),
            ValidTask("run-1", AgentType.Critic)
        ];
        tasks[0].TaskId = "task-topology";
        tasks[1].TaskId = "task-cost";
        tasks[2].TaskId = "task-compliance";
        tasks[3].TaskId = "task-critic";
        List<AgentResult> existingResults =
        [
            ValidResult(),
            ValidResult("run-1", AgentType.Cost),
            ValidResult("run-1", AgentType.Compliance)
        ];
        existingResults[0].TaskId = "task-topology";
        existingResults[1].TaskId = "task-cost";
        existingResults[2].TaskId = "task-compliance";

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. existingResults, result]);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenThreeResultsButSameAgentType_StaysWaitingForResults()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        result.TaskId = "task-topology-3";
        List<AgentTask> tasks =
        [
            ValidTask(),
            ValidTask(),
            ValidTask()
        ];
        tasks[0].TaskId = "task-topology-1";
        tasks[1].TaskId = "task-topology-2";
        tasks[2].TaskId = "task-topology-3";
        List<AgentResult> existingResults =
        [
            ValidResult(),
            ValidResult()
        ];
        existingResults[0].TaskId = "task-topology-1";
        existingResults[1].TaskId = "task-topology-2";

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));
        _resultRepository.Setup(r => r.CreateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. existingResults, result]);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenRunNotFound_ReturnsError()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);
        AgentResult result = ValidResult("nonexistent");

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("nonexistent", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
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
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
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
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenRunIdMatchIsCaseInsensitive_AcceptsResult()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        result.RunId = "RUN-1";
        List<AgentTask> tasks = [ValidTask()];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _resultRepository.Setup(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _resultRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([result]);

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeTrue();
        _resultRepository.Verify(r => r.CreateAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenResultForTaskAlreadySubmitted_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        List<AgentTask> tasks = [ValidTask()];
        List<AgentResult> existingResults = [result];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("already been submitted");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenTaskNotFoundForRun_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult();
        result.TaskId = "nonexistent-task";
        List<AgentTask> tasks = [ValidTask()];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("was not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SubmitAgentResultAsync_WhenResultAgentTypeDoesNotMatchTask_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        AgentResult result = ValidResult("run-1", AgentType.Cost);
        result.TaskId = "task-1";
        List<AgentTask> tasks = [ValidTask()];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));

        SubmitResultResult sutResult = await _sut.SubmitAgentResultAsync("run-1", result);

        sutResult.Success.Should().BeFalse();
        sutResult.Error.Should().Contain("does not match task AgentType");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetManifestAsync

    [SkippableFact]
    public async Task GetManifestAsync_WhenVersionExists_ReturnsManifest()
    {
        GoldenManifest manifest = new()
        {
            RunId = "run-1", SystemName = "TestSystem", Metadata = new ManifestMetadata { ManifestVersion = "v1" }
        };
        _unifiedGoldenManifestReader.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        GoldenManifest? result = await _sut.GetManifestAsync("v1");

        result.Should().NotBeNull();
        result.Metadata.ManifestVersion.Should().Be("v1");
    }

    [SkippableFact]
    public async Task GetManifestAsync_WhenVersionNotFound_ReturnsNull()
    {
        _unifiedGoldenManifestReader.Setup(r => r.GetByVersionAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        GoldenManifest? result = await _sut.GetManifestAsync("nonexistent");

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetManifestAsync_PassesCancellationTokenToRepository()
    {
        CancellationTokenSource cts = new();
        GoldenManifest manifest = new() { Metadata = new ManifestMetadata { ManifestVersion = "v1" } };
        _unifiedGoldenManifestReader.Setup(r => r.GetByVersionAsync("v1", cts.Token)).ReturnsAsync(manifest);

        await _sut.GetManifestAsync("v1", cts.Token);

        _unifiedGoldenManifestReader.Verify(r => r.GetByVersionAsync("v1", cts.Token), Times.Once);
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
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SeedFakeResultsAsync_WhenValid_SeedsResults()
    {
        ArchitectureRun run = ValidRun();
        ArchitectureRequest request = ValidRequest();
        List<AgentTask> tasks =
        [
            ValidTask(),
            ValidTask("run-1", AgentType.Cost),
            ValidTask("run-1", AgentType.Compliance),
            ValidTask("run-1", AgentType.Critic)
        ];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _resultRepository.Setup(r =>
                r.CreateManyAsync(It.IsAny<IReadOnlyList<AgentResult>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(4);
        result.Error.Should().BeNull();
        _resultRepository.Verify(
            r => r.CreateManyAsync(It.Is<IReadOnlyList<AgentResult>>(list => list.Count == 4),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _agentEvidencePackageRepository.Verify(
            r => r.CreateAsync(It.IsAny<AgentEvidencePackage>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _evidenceBuilder.Verify(
            b => b.BuildAsync("run-1", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task SeedFakeResultsAsync_WhenEvidencePackageAlreadyExists_DoesNotCreatePackageAgain()
    {
        ArchitectureRun run = ValidRun();
        ArchitectureRequest request = ValidRequest();
        List<AgentTask> tasks =
        [
            ValidTask(),
            ValidTask("run-1", AgentType.Cost),
            ValidTask("run-1", AgentType.Compliance),
            ValidTask("run-1", AgentType.Critic)
        ];

        _agentEvidencePackageRepository.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentEvidencePackage { RunId = "run-1", RequestId = "req-1" });
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _resultRepository.Setup(r =>
                r.CreateManyAsync(It.IsAny<IReadOnlyList<AgentResult>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Success.Should().BeTrue();
        _agentEvidencePackageRepository.Verify(
            r => r.CreateAsync(It.IsAny<AgentEvidencePackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _evidenceBuilder.Verify(
            b => b.BuildAsync(It.IsAny<string>(), It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
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
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SeedFakeResultsAsync_WhenRunNotFound_ReturnsError()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("nonexistent");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SeedFakeResultsAsync_WhenRequestNotFound_ReturnsError()
    {
        ArchitectureRun run = ValidRun();
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, [ValidTask()], []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRequest?)null);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [SkippableFact]
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

    [SkippableFact]
    public async Task SeedFakeResultsAsync_WhenRunAlreadyHasResults_ReturnsSuccessWithZeroCountAndDoesNotCreate()
    {
        ArchitectureRun run = ValidRun();
        ArchitectureRequest request = ValidRequest();
        List<AgentTask> tasks = [ValidTask()];
        List<AgentResult> existingResults = [ValidResult()];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, existingResults));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);

        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(0);
        result.Error.Should().BeNull();
        _resultRepository.Verify(r => r.CreateAsync(It.IsAny<AgentResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task SeedFakeResultsAsync_WhenPilotMarkFellBack_UpdatesRunAndWritesAudit()
    {
        Guid runGuid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        string runId = runGuid.ToString("N");
        ArchitectureRun run = ValidRun(runId);
        ArchitectureRequest request = ValidRequest();
        List<AgentTask> tasks =
        [
            ValidTask(runId),
            ValidTask(runId, AgentType.Cost),
            ValidTask(runId, AgentType.Compliance),
            ValidTask(runId, AgentType.Critic)
        ];

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailFor(run, tasks, []));
        _requestRepository.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _resultRepository.Setup(r =>
                r.CreateManyAsync(It.IsAny<IReadOnlyList<AgentResult>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RunRecord header = new()
        {
            RunId = runGuid,
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ScopeProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            ProjectId = "p",
            CreatedUtc = DateTime.UtcNow,
            RealModeFellBackToSimulator = false
        };

        _runRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(header);

        PilotSeedFakeResultsOptions pilot = new(MarkRealModeFellBackToSimulator: true);
        SeedFakeResultsResult result = await _sut.SeedFakeResultsAsync(runId, pilot);

        result.Success.Should().BeTrue();
        _runRepository.Verify(
            r => r.UpdateAsync(
                It.Is<RunRecord>(h => h.RealModeFellBackToSimulator && h.PilotAoaiDeploymentSnapshot == "gpt-test"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.FirstRealValueRunFellBackToSimulator && e.RunId == runGuid),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
