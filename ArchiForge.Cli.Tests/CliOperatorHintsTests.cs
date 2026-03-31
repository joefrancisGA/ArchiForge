using ArchiForge.Cli;

using FluentAssertions;

namespace ArchiForge.Cli.Tests;

/// <summary>
/// Focused tests for operator-facing stderr hints after API failures (56R hardening).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CliOperatorHintsTests
{
    [Fact]
    public void WriteAfterApiFailure_WhenCannotConnectMessage_writes_start_api_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(
            null,
            "Cannot connect to ArchiForge API: refused",
            stderr);

        string text = stderr.ToString();
        text.ToUpperInvariant().Should().Contain("ARCHIFORGE_API_URL");
        text.Should().Contain("ArchiForge.Api");
    }

    [Fact]
    public void WriteAfterApiFailure_WhenTimeoutMessage_writes_health_ready_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(null, "Request timed out.", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("health/ready");
    }

    [Fact]
    public void WriteAfterApiFailure_When503_writes_health_ready_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(503, "down", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("health/ready");
    }

    [Fact]
    public void WriteAfterApiFailure_When404_writes_scope_headers_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(404, "missing", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("x-tenant-id");
    }

    [Fact]
    public void WriteAfterReadinessFailed_writes_troubleshooting_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterReadinessFailed(stderr);

        string text = stderr.ToString().ToLowerInvariant();
        text.Should().Contain("/health/ready");
        text.Should().Contain("troubleshooting");
    }

    [Fact]
    public void WriteAfterHealthUnreachable_writes_ready_and_doctor_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterHealthUnreachable("http://localhost:5555", stderr);

        string text = stderr.ToString();
        text.Should().Contain("http://localhost:5555");
        text.ToLowerInvariant().Should().Contain("health/ready");
        text.ToLowerInvariant().Should().Contain("doctor");
    }
}
