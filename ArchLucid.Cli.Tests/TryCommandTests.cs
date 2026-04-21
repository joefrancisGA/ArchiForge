using ArchLucid.Cli.Commands;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
/// Unit tests for <see cref="TryCommand"/> and <see cref="TryCommandOptions"/>.
/// Covers the three guarantees the spec requires:
///  1. Argument parsing (defaults + flags + invalid input).
///  2. Missing-Docker handling (no <c>docker-compose.yml</c> in any ancestor of cwd).
///  3. Readiness-poll timeout (the polling helper returns the last observed status when the
///     deadline elapses without ever reaching <see cref="ArchitectureRunStatus.ReadyForCommit"/>).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TryCommandTests
{
    [Fact]
    public void Parse_NoArgs_ReturnsDefaults()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse([], out string? error);

        error.Should().BeNull();
        opts.Should().NotBeNull();
        opts.ApiBaseUrl.Should().Be(TryCommandOptions.DefaultApiBaseUrl);
        opts.UiBaseUrl.Should().Be(TryCommandOptions.DefaultUiBaseUrl);
        opts.OpenArtifacts.Should().BeTrue();
        opts.ReadinessDeadline.Should().Be(TryCommandOptions.DefaultReadinessDeadline);
        opts.CommitDeadline.Should().Be(TryCommandOptions.DefaultCommitDeadline);
    }

    [Fact]
    public void Parse_NoOpen_DisablesArtifactOpening()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse(["--no-open"], out string? error);

        error.Should().BeNull();
        opts!.OpenArtifacts.Should().BeFalse();
    }

    [Fact]
    public void Parse_OverridesUrlsAndDeadlines()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse(
            [
                "--api-base-url", "http://api.local:8080/",
                "--ui-base-url", "http://ui.local:9090",
                "--readiness-deadline", "60",
                "--commit-deadline", "30",
            ],
            out string? error);

        error.Should().BeNull();
        opts!.ApiBaseUrl.Should().Be("http://api.local:8080");
        opts.UiBaseUrl.Should().Be("http://ui.local:9090");
        opts.ReadinessDeadline.Should().Be(TimeSpan.FromSeconds(60));
        opts.CommitDeadline.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Parse_UnknownFlag_ReturnsUsageError()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse(["--bogus"], out string? error);

        opts.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace();
        error.Should().Contain("--bogus");
    }

    [Fact]
    public void Parse_FlagMissingValue_ReturnsUsageError()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse(["--api-base-url"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("Missing value for --api-base-url");
    }

    [Fact]
    public void Parse_NonNumericDeadline_ReturnsUsageError()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse(["--commit-deadline", "soon"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--commit-deadline");
    }

    [Fact]
    public void Parse_ZeroDeadline_ReturnsUsageError()
    {
        TryCommandOptions? opts = TryCommandOptions.Parse(["--readiness-deadline", "0"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--readiness-deadline");
    }

    [Fact]
    public async Task RunCoreAsync_WhenComposeNotFound_PrintsErrorAndReturnsUsageError()
    {
        StringWriter output = new();
        TryCommandOptions options = new();

        // Hooks where every step except FindComposeDirectory throws — proves the orchestrator short-circuits
        // before invoking Docker / API / browser when no docker-compose.yml is reachable from the cwd.
        TryCommandHooks hooks = MakeHooks(findCompose: () => null);

        int exit = await TryCommand.RunCoreAsync(options, hooks, output);

        exit.Should().Be(CliExitCode.UsageError);
        output.ToString().Should().Contain("docker-compose.yml not found");
    }

    [Fact]
    public async Task PollForCommittableStatusAsync_WhenStatusNeverReachesReadyForCommit_ReturnsLastObservedAfterDeadline()
    {
        int callCount = 0;

        ArchitectureRunStatus result = await TryCommand.PollForCommittableStatusAsync(
            probe: ct =>
            {
                callCount++;
                return Task.FromResult<ArchitectureRunStatus?>(ArchitectureRunStatus.WaitingForResults);
            },
            deadline: TimeSpan.FromMilliseconds(200),
            pollInterval: TimeSpan.FromMilliseconds(40),
            cancellationToken: CancellationToken.None);

        result.Should().Be(ArchitectureRunStatus.WaitingForResults);
        callCount.Should().BeGreaterThan(1, "the polling loop must iterate before giving up");
    }

    [Fact]
    public async Task PollForCommittableStatusAsync_ReturnsAsSoonAsStatusReachesReadyForCommit()
    {
        int callCount = 0;

        ArchitectureRunStatus result = await TryCommand.PollForCommittableStatusAsync(
            probe: ct =>
            {
                callCount++;
                ArchitectureRunStatus s = callCount switch
                {
                    1 => ArchitectureRunStatus.Created,
                    2 => ArchitectureRunStatus.WaitingForResults,
                    _ => ArchitectureRunStatus.ReadyForCommit,
                };
                return Task.FromResult<ArchitectureRunStatus?>(s);
            },
            deadline: TimeSpan.FromSeconds(5),
            pollInterval: TimeSpan.FromMilliseconds(20),
            cancellationToken: CancellationToken.None);

        result.Should().Be(ArchitectureRunStatus.ReadyForCommit);
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task PollForCommittableStatusAsync_NullProbeResults_DoNotShortCircuitDeadline()
    {
        ArchitectureRunStatus result = await TryCommand.PollForCommittableStatusAsync(
            probe: ct => Task.FromResult<ArchitectureRunStatus?>(null),
            deadline: TimeSpan.FromMilliseconds(120),
            pollInterval: TimeSpan.FromMilliseconds(30),
            cancellationToken: CancellationToken.None);

        // Never observed a status => returns the initial "Created" sentinel.
        result.Should().Be(ArchitectureRunStatus.Created);
    }

    [Fact]
    public void PollForCommittableStatusAsync_RejectsNonPositiveDeadline()
    {
        Func<Task> act = () => TryCommand.PollForCommittableStatusAsync(
            probe: ct => Task.FromResult<ArchitectureRunStatus?>(ArchitectureRunStatus.Committed),
            deadline: TimeSpan.Zero,
            pollInterval: TimeSpan.FromMilliseconds(10),
            cancellationToken: CancellationToken.None);

        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Build a hooks bundle whose entries all throw if invoked. Tests override individual hooks via
    /// optional parameters when they need a specific path to be exercised.
    /// </summary>
    private static TryCommandHooks MakeHooks(
        Func<string?>? findCompose = null,
        Func<CancellationToken, Task<int>>? pilotUp = null)
    {
        return new TryCommandHooks
        {
            FindComposeDirectory = findCompose ?? (() => throw new InvalidOperationException("FindComposeDirectory should not have been invoked.")),
            PilotUp = pilotUp ?? (_ => throw new InvalidOperationException("PilotUp should not have been invoked.")),
            DemoSeed = (_, __) => throw new InvalidOperationException("DemoSeed should not have been invoked."),
            CreateRun = (_, __) => throw new InvalidOperationException("CreateRun should not have been invoked."),
            ExecuteRun = (_, __, ___) => throw new InvalidOperationException("ExecuteRun should not have been invoked."),
            GetRun = (_, __, ___) => throw new InvalidOperationException("GetRun should not have been invoked."),
            SeedFakeResults = (_, __, ___) => throw new InvalidOperationException("SeedFakeResults should not have been invoked."),
            CommitRun = (_, __, ___) => throw new InvalidOperationException("CommitRun should not have been invoked."),
            DownloadFirstValueReport = (_, __, ___, ____) => throw new InvalidOperationException("DownloadFirstValueReport should not have been invoked."),
            OpenFile = _ => throw new InvalidOperationException("OpenFile should not have been invoked."),
            OpenUrl = _ => throw new InvalidOperationException("OpenUrl should not have been invoked."),
            CreateApiClient = _ => throw new InvalidOperationException("CreateApiClient should not have been invoked."),
        };
    }
}
