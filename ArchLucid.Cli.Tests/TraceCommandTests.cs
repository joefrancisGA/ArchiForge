using ArchLucid.Cli;
using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>Unit tests for <see cref="TraceCommand"/> output and URL building (no live API).</summary>
[Trait("Category", "Unit")]
public sealed class TraceCommandTests
{
    [Fact]
    public async Task RunCoreAsync_WhenOtelTraceIdAndTemplateSet_WritesEncodedTraceViewerUrl()
    {
        StringWriter output = new();
        string runId = "a1b2c3d4e5f6789012345678901234ab";
        string traceId = "trace+id/with special";
        string template = "https://grafana.example.com/explore?traceId={traceId}&other={traceId}";

        int exitCode = await TraceCommand.RunCoreAsync(
            runId,
            _ => Task.FromResult<ArchLucidApiClient.GetRunResult?>(
                new ArchLucidApiClient.GetRunResult
                {
                    Run = new ArchLucidApiClient.RunInfo { RunId = runId, OtelTraceId = traceId },
                }),
            () => template,
            () => false,
            output,
            openBrowser: null);

        exitCode.Should().Be(0);
        string expected = TraceCommand.BuildTraceViewerUrl(template, traceId);
        output.ToString().TrimEnd().Should().Be(expected);
    }

    [Fact]
    public async Task RunCoreAsync_WhenOtelTraceIdMissing_WritesNoTraceMessage()
    {
        StringWriter output = new();
        string runId = "run-xyz";

        int exitCode = await TraceCommand.RunCoreAsync(
            runId,
            _ => Task.FromResult<ArchLucidApiClient.GetRunResult?>(
                new ArchLucidApiClient.GetRunResult
                {
                    Run = new ArchLucidApiClient.RunInfo { RunId = runId, OtelTraceId = null },
                }),
            () => "https://x/{traceId}",
            () => false,
            output,
            openBrowser: null);

        exitCode.Should().Be(0);
        string text = output.ToString();
        text.Should().Contain($"No trace ID recorded for run {runId}");
        text.Should().Contain("predate trace persistence");
    }

    [Fact]
    public async Task RunCoreAsync_WhenTemplateUnset_WritesRawTraceIdAndInstructions()
    {
        StringWriter output = new();
        string runId = "run-1";
        string traceId = "deadbeefdeadbeefdeadbeefdeadbeef";

        int exitCode = await TraceCommand.RunCoreAsync(
            runId,
            _ => Task.FromResult<ArchLucidApiClient.GetRunResult?>(
                new ArchLucidApiClient.GetRunResult
                {
                    Run = new ArchLucidApiClient.RunInfo { RunId = runId, OtelTraceId = traceId },
                }),
            () => null,
            () => false,
            output,
            openBrowser: null);

        exitCode.Should().Be(0);
        string text = output.ToString();
        text.Should().Contain(traceId);
        text.Should().Contain("ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE");
        text.Should().Contain("https://grafana.example.com/explore?traceId={traceId}");
    }
}
