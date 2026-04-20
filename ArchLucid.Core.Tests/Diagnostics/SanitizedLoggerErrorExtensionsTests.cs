using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SanitizedLoggerErrorExtensionsTests
{
    [Fact]
    public void LogErrorUnhandledWorkerHttpRequest_strips_control_chars_and_passes_exception()
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

        mock.Object.LogErrorUnhandledWorkerHttpRequest(ex, "GET\n", "/path\t/x");

        rendered.Should().NotBeNull();
        string text = rendered!;

        text.Should().Contain("GET_");
        text.Should().Contain("/path_/x");
        text.Should().NotContain("\n");
        text.Should().NotContain("\t");
        capturedEx.Should().BeSameAs(ex);
    }

    [Fact]
    public void LogErrorUnhandledWorkerHttpRequest_throws_when_logger_null()
    {
        ILogger logger = null!;

        Action act = () => logger.LogErrorUnhandledWorkerHttpRequest(new InvalidOperationException("x"), "GET", "/");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogErrorUnhandledWorkerHttpRequest_throws_when_exception_null()
    {
        Mock<ILogger> mock = new();

        Action act = () => mock.Object.LogErrorUnhandledWorkerHttpRequest(null!, "GET", "/");

        act.Should().Throw<ArgumentNullException>().WithParameterName("ex");
    }
}
