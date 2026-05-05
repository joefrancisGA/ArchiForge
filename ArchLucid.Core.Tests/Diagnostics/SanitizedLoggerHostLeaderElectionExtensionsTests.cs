using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SanitizedLoggerHostLeaderElectionExtensionsTests
{
    [Fact]
    public void LogDebugHostLeaderLeaseNotHeldFollowerWait_scrubs_control_chars_in_lease_and_logs_ms()
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

        SanitizedLoggerHostLeaderElectionExtensions.LogDebugHostLeaderLeaseNotHeldFollowerWait(
            mock.Object,
            "lease\nkey",
            500);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("lease_key");
        text.Should().Contain("500");
        text.Should().NotContain("\n");
    }

    [Fact]
    public void LogInformationHostLeaderLeaseAcquired_scrubs_both_placeholders()
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

        SanitizedLoggerHostLeaderElectionExtensions.LogInformationHostLeaderLeaseAcquired(mock.Object, "l\n1", "i\t2");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("l_1");
        text.Should().Contain("i_2");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
    }

    [Fact]
    public void LogInformationHostLeaderWorkStoppedLeaseLossOrHandoff_scrubs_lease()
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

        SanitizedLoggerHostLeaderElectionExtensions.LogInformationHostLeaderWorkStoppedLeaseLossOrHandoff(
            mock.Object,
            "x\rname");

        rendered.Should().NotBeNull();

        rendered!.Should().Contain("x_name");
        rendered.Should().NotContain("\r");
    }

    [Fact]
    public void LogWarningHostLeaderLeaseRenewalFailedStopping_scrubs_both_placeholders()
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

        SanitizedLoggerHostLeaderElectionExtensions.LogWarningHostLeaderLeaseRenewalFailedStopping(
            mock.Object,
            "lease\n",
            "inst\t");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("lease_");
        text.Should().Contain("inst_");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
    }

    [Fact]
    public void LogDebugHostLeaderLeaseNotHeldFollowerWait_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () =>
            SanitizedLoggerHostLeaderElectionExtensions.LogDebugHostLeaderLeaseNotHeldFollowerWait(logger, "l", 1);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
