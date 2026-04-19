using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Host.Composition.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchLucidJobRunnerTests
{
    [Fact]
    public async Task RunNamedJobAsync_unknown_name_returns_exit_code_3()
    {
        ArchLucidJobRunner runner = new(
            [new StubJob("known", ArchLucidJobExitCodes.Success)],
            new JobRunTelemetry(NullLogger<JobRunTelemetry>.Instance),
            NullLogger<ArchLucidJobRunner>.Instance);

        int code = await runner.RunNamedJobAsync("missing-job", CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.UnknownJob);
    }

    [Fact]
    public async Task RunNamedJobAsync_invokes_matching_job()
    {
        StubJob stub = new("alpha", ArchLucidJobExitCodes.Success);
        ArchLucidJobRunner runner = new(
            [stub],
            new JobRunTelemetry(NullLogger<JobRunTelemetry>.Instance),
            NullLogger<ArchLucidJobRunner>.Instance);

        int code = await runner.RunNamedJobAsync("ALPHA", CancellationToken.None);

        code.Should().Be(ArchLucidJobExitCodes.Success);
        stub.Invoked.Should().BeTrue();
    }

    private sealed class StubJob(string name, int exitCode) : IArchLucidJob
    {
        public bool Invoked { get; private set; }

        public string Name => name;

        public Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            Invoked = true;

            return Task.FromResult(exitCode);
        }
    }
}
