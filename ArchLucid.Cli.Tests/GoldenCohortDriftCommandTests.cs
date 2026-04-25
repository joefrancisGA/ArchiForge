using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class GoldenCohortDriftCommandTests
{
    [Fact]
    public async Task RunAsync_refuses_strict_real_without_real_llm_context_env()
    {
        string? prevReal = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM");
        string? prevMode = Environment.GetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE");
        string? prevApp = Environment.GetEnvironmentVariable("AgentExecution__Mode");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", null);
            Environment.SetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE", "Simulator");
            Environment.SetEnvironmentVariable("AgentExecution__Mode", "Simulator");

            int code = await GoldenCohortDriftCommand.RunAsync(["--strict-real", "--structural-only"]);

            code.Should().Be(CliExitCode.UsageError);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", prevReal);
            Environment.SetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE", prevMode);
            Environment.SetEnvironmentVariable("AgentExecution__Mode", prevApp);
        }
    }
}
