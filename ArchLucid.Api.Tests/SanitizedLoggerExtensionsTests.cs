using ArchLucid.Api.Logging;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="SanitizedLoggerExtensions" /> (CWE-117 helper used by API controllers).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SanitizedLoggerExtensionsTests
{
    [Fact]
    public void LogWarningWithSanitizedUserArg_substitutes_sanitized_value_in_message()
    {
        Mock<ILogger> mock = new();
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

        InvalidOperationException ex = new("Promote rejected");
        mock.Object.LogWarningWithSanitizedUserArg(
            ex,
            "Promote failed for run '{RunId}'.",
            "run\nid");

        rendered.Should().Be("Promote failed for run 'run_id'.");
    }

    [Fact]
    public void LogWarningWithSanitizedUserArg_null_user_value_renders_empty_placeholder()
    {
        Mock<ILogger> mock = new();
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

        mock.Object.LogWarningWithSanitizedUserArg(null, "Promote failed for run '{RunId}'.", null);

        rendered.Should().Be("Promote failed for run ''.");
    }
}
