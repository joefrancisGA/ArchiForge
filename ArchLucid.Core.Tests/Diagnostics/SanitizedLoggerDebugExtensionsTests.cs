using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SanitizedLoggerDebugExtensionsTests
{
    [Fact]
    public void LogDebugAgentTaskFinished_strips_control_chars_in_user_strings()
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
                Delegate formatter = (Delegate)invocation.Arguments[4]!;
                object state = invocation.Arguments[2]!;
                object? ex = invocation.Arguments[3];
                rendered = formatter.DynamicInvoke(state, ex) as string;
            }));

        mock.Object.LogDebugAgentTaskFinished("run\nid", "task\t1", "key\rX", 99);

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("run_id");
        text.Should().Contain("task_1");
        text.Should().Contain("key_X");
        text.Should().Contain("99");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
        text.Should().NotContain("\r");
    }

    [Fact]
    public void LogDebugAgentTaskFinished_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogDebugAgentTaskFinished("a", "b", "c", 0);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
