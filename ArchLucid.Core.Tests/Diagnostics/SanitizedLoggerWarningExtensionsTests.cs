using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SanitizedLoggerWarningExtensionsTests
{
    [Fact]
    public void LogWarningWithTwoSanitizedUserStrings_strips_control_chars()
    {
        Mock<ILogger> mock = new();
        mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        string? rendered = null;

        mock.Setup(m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                Delegate formatter = (Delegate)invocation.Arguments[4];
                object state = invocation.Arguments[2];
                object ex = invocation.Arguments[3];
                rendered = formatter.DynamicInvoke(state, ex) as string;
            }));

        mock.Object.LogWarningWithTwoSanitizedUserStrings(
            "Run {RunId}: {Gaps}",
            "run\nid",
            "a\tb");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("run_id");
        text.Should().Contain("a_b");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
    }

    [Fact]
    public void LogWarningWithThreeSanitizedUserStrings_strips_control_chars()
    {
        Mock<ILogger> mock = new();
        mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        string? rendered = null;

        mock.Setup(m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                Delegate formatter = (Delegate)invocation.Arguments[4];
                object state = invocation.Arguments[2];
                object ex = invocation.Arguments[3];
                rendered = formatter.DynamicInvoke(state, ex) as string;
            }));

        mock.Object.LogWarningWithThreeSanitizedUserStrings(
            "A={A} B={B} C={C}",
            "x\n1",
            "y\t2",
            "z\r3");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("x_1");
        text.Should().Contain("y_2");
        text.Should().Contain("z_3");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
        text.Should().NotContain("\r");
    }

    [Fact]
    public void LogWarningWithThreeSanitizedUserStrings_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogWarningWithThreeSanitizedUserStrings("x {A} {B} {C}", "a", "b", "c");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogWarningWithTwoSanitizedUserStrings_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogWarningWithTwoSanitizedUserStrings("x {A} {B}", "a", "b");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogWarningComparisonReplayFailed_strips_control_chars_and_passes_exception()
    {
        Mock<ILogger> mock = new();
        mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        string? rendered = null;
        Exception? capturedEx = null;

        mock.Setup(m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                Delegate formatter = (Delegate)invocation.Arguments[4];
                object state = invocation.Arguments[2];
                capturedEx = invocation.Arguments[3] as Exception;
                rendered = formatter.DynamicInvoke(state, capturedEx) as string;
            }));

        InvalidOperationException ex = new("boom\nline");

        mock.Object.LogWarningComparisonReplayFailed(
            ex,
            "rec\tid",
            false,
            true,
            ex.Message);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("rec_id");
        text.Should().Contain("boom_line");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
        capturedEx.Should().BeSameAs(ex);
    }

    [Fact]
    public void LogWarningComparisonReplayFailed_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogWarningComparisonReplayFailed(
            new InvalidOperationException("x"),
            "id",
            false,
            false,
            "m");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogWarningComparisonReplayFailed_throws_when_exception_null()
    {
        Mock<ILogger> mock = new();

        Action act = () => mock.Object.LogWarningComparisonReplayFailed(
            null!,
            "id",
            false,
            false,
            "m");

        act.Should().Throw<ArgumentNullException>().WithParameterName("ex");
    }

    [Fact]
    public void LogWarningArchitectureRunExecutionFailed_strips_control_chars_and_passes_exception()
    {
        Mock<ILogger> mock = new();
        mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        string? rendered = null;
        Exception? capturedEx = null;

        mock.Setup(m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                Delegate formatter = (Delegate)invocation.Arguments[4];
                object state = invocation.Arguments[2];
                capturedEx = invocation.Arguments[3] as Exception;
                rendered = formatter.DynamicInvoke(state, capturedEx) as string;
            }));

        InvalidOperationException ex = new("boom");

        mock.Object.LogWarningArchitectureRunExecutionFailed(ex, "run\nid", "ExType\rName");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("Architecture run execution failed:");
        text.Should().Contain("run_id");
        text.Should().Contain("ExType_Name");
        text.Should().NotContain("\n");
        text.Should().NotContain("\r");
        capturedEx.Should().BeSameAs(ex);
    }

    [Fact]
    public void LogWarningArchitectureRunExecutionFailed_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogWarningArchitectureRunExecutionFailed(new InvalidOperationException("x"), "r", "T");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogWarningArchitectureRunExecutionFailed_throws_when_exception_null()
    {
        Mock<ILogger> mock = new();

        Action act = () => mock.Object.LogWarningArchitectureRunExecutionFailed(null!, "r", "T");

        act.Should().Throw<ArgumentNullException>().WithParameterName("ex");
    }

    [Fact]
    public void LogWarningOperatorShellClientError_sanitizes_all_placeholders()
    {
        Mock<ILogger> mock = new();
        mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        string? rendered = null;

        mock.Setup(m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                Delegate formatter = (Delegate)invocation.Arguments[4];
                object state = invocation.Arguments[2];
                object ex = invocation.Arguments[3];
                rendered = formatter.DynamicInvoke(state, ex) as string;
            }));

        mock.Object.LogWarningOperatorShellClientError(
            "msg\n1",
            "/p\tth",
            "ua\rx",
            "ts\n2",
            "st\tack");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("Operator shell client error:");
        text.Should().Contain("msg_1");
        text.Should().Contain("p_th");
        text.Should().Contain("ua_x");
        text.Should().Contain("ts_2");
        text.Should().Contain("st_ack");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
        text.Should().NotContain("\r");
    }

    [Fact]
    public void LogWarningOperatorShellClientError_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogWarningOperatorShellClientError("m", "p", "u", "t", "s");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
