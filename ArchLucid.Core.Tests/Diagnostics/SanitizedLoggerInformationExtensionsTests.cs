using ArchLucid.Contracts.Common;
using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SanitizedLoggerInformationExtensionsTests
{
    [Fact]
    public void LogInformationArchitectureRunCommitted_strips_control_chars_in_user_strings()
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

        mock.Object.LogInformationArchitectureRunCommitted("run\nid", "v1\t2", 7);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("run_id");
        text.Should().Contain("v1_2");
        text.Should().Contain("7");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
    }

    [Fact]
    public void LogInformationCommitRunIdempotentReturn_strips_control_chars_in_user_strings()
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

        mock.Object.LogInformationCommitRunIdempotentReturn("a\rb", "c\nc", 2);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("a_b");
        text.Should().Contain("c_c");
        text.Should().Contain("2");
        text.Should().NotContain("\r");
        text.Should().NotContain("\n");
    }

    [Fact]
    public void LogInformationArchitectureRunCommitted_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogInformationArchitectureRunCommitted("x", "y", 0);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogInformationComparisonReplaySucceeded_strips_control_chars_in_user_strings()
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

        mock.Object.LogInformationComparisonReplaySucceeded(
            "cmp\nid",
            "type\tX",
            "fmt\rY",
            "mode\nZ",
            true,
            false,
            42,
            true);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("cmp_id");
        text.Should().Contain("type_X");
        text.Should().Contain("fmt_Y");
        text.Should().Contain("mode_Z");
        text.Should().Contain("42");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
        text.Should().NotContain("\r");
    }

    [Fact]
    public void LogInformationAgentExecutionBatchStarting_strips_control_chars_in_user_strings()
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

        mock.Object.LogInformationAgentExecutionBatchStarting("run\nid", "a\tb,c", 3);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("run_id");
        text.Should().Contain("a_b");
        text.Should().Contain("3");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
    }

    [Fact]
    public void LogInformationAgentExecutionBatchCompleted_strips_control_chars_in_run_id()
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

        mock.Object.LogInformationAgentExecutionBatchCompleted("x\ry", 5);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("x_y");
        text.Should().Contain("5");
        text.Should().NotContain("\r");
    }

    [Fact]
    public void LogInformationAgentExecutionBatchStarting_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogInformationAgentExecutionBatchStarting("r", "t", 1);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogInformationAgentResultSubmitted_strips_control_chars_in_user_strings()
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

        mock.Object.LogInformationAgentResultSubmitted(
            "run\nid",
            "res\tid",
            AgentType.Topology,
            ArchitectureRunStatus.ReadyForCommit);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("run_id");
        text.Should().Contain("res_id");
        text.Should().Contain("Topology");
        text.Should().Contain("ReadyForCommit");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
    }

    [Fact]
    public void LogInformationAgentResultSubmitted_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () =>
            logger.LogInformationAgentResultSubmitted("r", "s", AgentType.Cost,
                ArchitectureRunStatus.WaitingForResults);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
