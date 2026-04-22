using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class GoldenCohortLockBaselineCommandTests
{
    [Fact]
    public async Task RunAsync_refuses_when_real_llm_flag_set()
    {
        string? prev = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", "true");

            int code = await GoldenCohortLockBaselineCommand.RunAsync([]);

            code.Should().Be(CliExitCode.UsageError);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", prev);
        }
    }

    [Fact]
    public async Task RunAsync_refuses_write_without_owner_approval_env()
    {
        string? prevReal = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM");
        string? prevApproved = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", null);
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED", null);

            int code = await GoldenCohortLockBaselineCommand.RunAsync(["--write"]);

            code.Should().Be(CliExitCode.UsageError);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", prevReal);
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED", prevApproved);
        }
    }

    [Fact]
    public async Task RunAsync_refuses_when_agent_execution_mode_real_in_shell()
    {
        string? prevMode = Environment.GetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE");
        string? prevReal = Environment.GetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", null);
            Environment.SetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE", "Real");

            int code = await GoldenCohortLockBaselineCommand.RunAsync([]);

            code.Should().Be(CliExitCode.UsageError);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_AGENT_EXECUTION_MODE", prevMode);
            Environment.SetEnvironmentVariable("ARCHLUCID_GOLDEN_COHORT_REAL_LLM", prevReal);
        }
    }
}
