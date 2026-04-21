using ArchLucid.Application.Explanation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Explanation;

public sealed class FindingLlmAuditServiceTests
{
    private static readonly Guid RunGuid = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact]
    public async Task BuildAsync_uses_SourceAgentExecutionTraceId_when_present()
    {
        Finding finding = new()
        {
            FindingId = "f1",
            EngineType = nameof(AgentType.Topology),
            Trace = new ExplainabilityTrace { SourceAgentExecutionTraceId = "abc123" },
        };

        AgentExecutionTrace trace = new()
        {
            TraceId = "abc123",
            RunId = RunGuid.ToString("N"),
            AgentType = AgentType.Topology,
            SystemPrompt = "sys",
            UserPrompt = "user@test.com",
            RawResponse = "ok",
        };

        Mock<IAuthorityQueryService> authority = new();
        authority.Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), RunGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunDetailDto
            {
                FindingsSnapshot = new FindingsSnapshot { Findings = [finding] },
            });

        Mock<IAgentExecutionTraceRepository> traces = new();
        traces.Setup(t => t.GetByTraceIdAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(trace);

        Mock<IPromptRedactor> redactor = new();
        redactor.Setup(r => r.Redact(It.IsAny<string?>())).Returns((string? s) => new PromptRedactionOutcome(s ?? "", new Dictionary<string, int>()));

        FindingLlmAuditService sut = new(
            authority.Object,
            Mock.Of<IScopeContextProvider>(p => p.GetCurrentScope() == new ScopeContext()),
            traces.Object,
            redactor.Object);

        ArchLucid.Contracts.Explanation.FindingLlmAuditResult? result =
            await sut.BuildAsync(RunGuid, "f1", CancellationToken.None);

        result.Should().NotBeNull();
        result.TraceId.Should().Be("abc123");
        result.UserPromptRedacted.Should().Be("user@test.com");
        traces.Verify(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuildAsync_falls_back_to_first_matching_AgentType()
    {
        Finding finding = new()
        {
            FindingId = "f1",
            EngineType = nameof(AgentType.Cost),
            Trace = new ExplainabilityTrace(),
        };

        AgentExecutionTrace wrong = new()
        {
            TraceId = "w1",
            AgentType = AgentType.Topology,
            SystemPrompt = "a",
            UserPrompt = "b",
            RawResponse = "c",
        };

        AgentExecutionTrace match = new()
        {
            TraceId = "m1",
            AgentType = AgentType.Cost,
            SystemPrompt = "s",
            UserPrompt = "u",
            RawResponse = "r",
        };

        Mock<IAuthorityQueryService> authority = new();
        authority.Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), RunGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunDetailDto
            {
                FindingsSnapshot = new FindingsSnapshot { Findings = [finding] },
            });

        Mock<IAgentExecutionTraceRepository> traces = new();
        traces.Setup(t => t.GetByRunIdAsync(RunGuid.ToString("N"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentExecutionTrace> { wrong, match });

        Mock<IPromptRedactor> redactor = new();
        redactor.Setup(r => r.Redact(It.IsAny<string?>())).Returns((string? s) => new PromptRedactionOutcome(s ?? "", new Dictionary<string, int>()));

        FindingLlmAuditService sut = new(
            authority.Object,
            Mock.Of<IScopeContextProvider>(p => p.GetCurrentScope() == new ScopeContext()),
            traces.Object,
            redactor.Object);

        ArchLucid.Contracts.Explanation.FindingLlmAuditResult? result =
            await sut.BuildAsync(RunGuid, "f1", CancellationToken.None);

        result.Should().NotBeNull();
        result.TraceId.Should().Be("m1");
    }
}
