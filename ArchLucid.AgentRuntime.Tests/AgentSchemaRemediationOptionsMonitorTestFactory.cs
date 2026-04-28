using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

internal static class AgentSchemaRemediationOptionsMonitorTestFactory
{
    /// <summary>
    ///     Returns a frozen <see cref="IOptionsMonitor{T}" /> snapshot for handlers under test. Defaults to a single completion
    ///     attempt so tests behave like pre-remediation callers (no retry loop unless configured).
    /// </summary>
    internal static IOptionsMonitor<AgentSchemaRemediationOptions> Create(int maxCompletionAttempts = 1)
    {
        AgentSchemaRemediationOptions options = new()
        {
            MaxCompletionAttempts = maxCompletionAttempts
        };
        options.Normalize();

        Mock<IOptionsMonitor<AgentSchemaRemediationOptions>> mock = new();
        mock.Setup(o => o.CurrentValue).Returns(options);
        return mock.Object;
    }
}
