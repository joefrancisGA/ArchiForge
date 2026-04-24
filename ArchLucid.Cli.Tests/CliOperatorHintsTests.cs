using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Focused tests for operator-facing stderr hints after API failures (56R hardening).
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
            "Cannot connect to ArchLucid API: refused",
            stderr);

        string text = stderr.ToString();
        text.ToUpperInvariant().Should().Contain("ARCHLUCID_API_URL");
        text.Should().Contain("ArchLucid.Api");
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
        text.Should().Contain("/version");
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

    [Fact]
    public void WriteAfterApiFailure_When500_writes_correlation_and_logs_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(500, "internal", stderr);

        string text = stderr.ToString().ToLowerInvariant();
        text.Should().Contain("correlation");
        text.Should().Contain("runid");
    }

    [Fact]
    public void WriteAfterApiFailure_When401_writes_auth_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(401, "x", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("jwt");
    }

    [Fact]
    public void WriteAfterApiFailure_When403_writes_identity_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(403, "x", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("reader");
    }

    [Fact]
    public void WriteAfterApiFailure_When409_writes_idempotency_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(409, "x", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("idempotency");
    }

    [Fact]
    public void WriteAfterApiFailure_When400_writes_body_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(400, "x", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("problem");
    }

    [Fact]
    public void WriteAfterApiFailure_When422_writes_body_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(422, "x", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("problem");
    }

    [Fact]
    public void WriteAfterApiFailure_When429_writes_retry_hint()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(429, "x", stderr);

        stderr.ToString().ToLowerInvariant().Should().Contain("retry");
    }

    [Fact]
    public void WriteAfterApiFailure_When418_writes_nothing_extra()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteAfterApiFailure(418, "x", stderr);

        stderr.ToString().Should().BeEmpty();
    }

    [Fact]
    public void WriteBriefMissingHint_writes_path_and_inputs_brief()
    {
        StringWriter stderr = new();

        CliOperatorHints.WriteBriefMissingHint("inputs/brief.md", stderr);

        string text = stderr.ToString();
        text.Should().Contain("inputs/brief.md");
        text.ToLowerInvariant().Should().Contain("archlucid.json");
    }
}
